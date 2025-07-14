// My Profile JavaScript
$(document).ready(async function () {
  if (window.adminAuth) {
    await window.adminAuth.updateUserProfileDisplay();
  }
});

document.addEventListener("DOMContentLoaded", async function () {
  function getToken() {
    if (typeof window.getToken === "function") {
      return window.getToken();
    }
    return localStorage.getItem("authToken");
  }

  function parseJwt(token) {
    if (typeof window.parseJwt === "function") {
      return window.parseJwt(token);
    }
    if (!token) return null;
    try {
      return JSON.parse(
        atob(token.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")),
      );
    } catch {
      return null;
    }
  }

  function getCurrentUserInfo() {
    if (
      window.adminAuth &&
      typeof window.adminAuth.getCurrentUserInfo === "function"
    ) {
      return window.adminAuth.getCurrentUserInfo();
    }
    const token = getToken();
    return parseJwt(token);
  }

  function getAuthHeaders() {
    const token = getToken();
    return token
      ? { Authorization: "Bearer " + token, "Content-Type": "application/json" }
      : { "Content-Type": "application/json" };
  }

  const API_BASE = "http://localhost:5050/api/User";

  const form = document.getElementById("editUserForm");
  const removeProfilePictureBtn = document.getElementById(
    "remove-profile-picture",
  );
  const profilePictureInput = document.getElementById("edit-profilePicture");
  const cropImageBtn = document.getElementById("cropImageBtn");

  async function loadUserInfo() {
    let user = getCurrentUserInfo();
    if (!user || !user.id) {
      try {
        const res = await fetch(`${API_BASE}`, { headers: getAuthHeaders() });
        const data = await res.json();
        if (data.success && data.data && data.data.length > 0) {
          user = data.data[0];
        } else {
          toastr.error("User not found or invalid token");
          return;
        }
      } catch (e) {
        toastr.error("User not found or invalid token");
        return;
      }
    }
    try {
      const res = await fetch(`${API_BASE}/${user.id}`, {
        headers: getAuthHeaders(),
      });
      const data = await res.json();
      if (data.success && data.data) {
        const u = data.data;
        $("#edit-username")
          .val(u.username || "")
          .prop("disabled", true);
        $("#edit-email")
          .val(u.email || "")
          .prop("disabled", true);
        $("#edit-fullName").val(u.fullName || "");
        $("#edit-phoneNumber").val(u.phoneNumber || "");
        $("#edit-dateOfBirth").val(
          u.dateOfBirth ? u.dateOfBirth.split("T")[0] : "",
        );
        $("#edit-address").val(u.address || "");
        $("#edit-bio").val(u.bio || "");
        if (u.profilePicture) {
          $("#edit-profilePicture-preview").attr("src", u.profilePicture);
          $("#edit-profilePicture-container").show();
        } else {
          $("#edit-profilePicture-container").hide();
        }
        $("#edit-profilePicture").val("");
        window._editProfilePictureBase64 = null;
        window._profilePictureRemoved = false;
        $("#editUserForm").data("original", {
          fullName: u.fullName || "",
          phoneNumber: u.phoneNumber || "",
          dateOfBirth: u.dateOfBirth || "",
          address: u.address || "",
          bio: u.bio || "",
        });
        $("#editUserForm").data("userid", u.id);
      } else {
        toastr.error(data.message || "Failed to load user info");
      }
    } catch (e) {
      toastr.error("Failed to load user info");
    }
  }

  // --- CropperJS integration ---
  let cropper = null;
  let selectedImageFile = null;
  let windowCropper = null;
  let currentZoom = 1;
  let cropperReady = false;
  let initialCropBoxWidth = null;

  $(document).on(
    "change",
    "#edit-profilePicture, #profile-picture-input",
    function (e) {
      const file = this.files && this.files[0];
      if (file) {
        if (!file.type.startsWith("image/")) {
          toastr.error("Please select a valid image file!");
          this.value = "";
          return;
        }
        if (file.size > 5 * 1024 * 1024) {
          toastr.error("Image size must be less than 5MB!");
          this.value = "";
          return;
        }
        if (file.size < 10 * 1024) {
          toastr.warning(
            "Image size is very small. For better quality, use an image larger than 10KB.",
          );
        }
        selectedImageFile = file;
        const reader = new FileReader();
        reader.onload = function (ev) {
          const $img = $("#cropper-image");
          $img.attr("src", ev.target.result);
          $img.off("load").on("load", function () {
            if (cropper) {
              cropper.destroy();
              cropper = null;
            }
            $("#cropImageModal").modal("show");
            $("#cropImageModal").one("shown.bs.modal", function () {
              setTimeout(() => {
                initializeCropper($img[0], ev.target.result);
              }, 100);
            });
          });
          if ($img[0].complete) {
            $img.trigger("load");
          }
        };
        reader.readAsDataURL(file);
      } else {
        $("#edit-profilePicture-container, #profile-picture-preview").hide();
        window._editProfilePictureBase64 = null;
      }
    },
  );

  $(document)
    .off("click", "#cropImageBtn")
    .on("click", "#cropImageBtn", function () {
      if (!cropper || !cropperReady) {
        toastr.error("Cropper is not ready. Please wait for the image to load.");
        return;
      }
      let cropBox = cropper.getCropBoxData();
      const imageData = cropper.getImageData();
      if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) {
        toastr.error("Crop area is invalid. Please adjust the crop box.");
        return;
      }
      if (imageData && (cropBox.width > imageData.naturalWidth || cropBox.height > imageData.naturalHeight)) {
        toastr.error("Crop area is larger than the image. Please zoom in or use a larger image.");
        return;
      }
      try {
        const canvas = cropper.getCroppedCanvas({
          width: 200,
          height: 200,
          imageSmoothingEnabled: true,
          imageSmoothingQuality: "high",
          fillColor: "#fff",
        });
        if (!canvas) throw new Error("Canvas is null. Image may be too small or crop area invalid.");
        const size = 200;
        const circleCanvas = document.createElement("canvas");
        circleCanvas.width = size;
        circleCanvas.height = size;
        const ctx = circleCanvas.getContext("2d");
        ctx.save();
        ctx.beginPath();
        ctx.arc(size / 2, size / 2, size / 2, 0, 2 * Math.PI);
        ctx.closePath();
        ctx.clip();
        ctx.drawImage(canvas, 0, 0, size, size);
        ctx.restore();
        window._editProfilePictureBase64 = circleCanvas.toDataURL("image/png");
        $("#crop-avatar-preview, #edit-profilePicture-preview").attr(
          "src",
          window._editProfilePictureBase64,
        );
        $("#edit-profilePicture-container, #profile-picture-preview").show();
        setTimeout(() => {
          $("#cropImageModal").modal("hide");
          toastr.success(
            "Profile picture cropped successfully! The image will be displayed as a circular avatar.",
          );
        }, 200);
      } catch (error) {
        window._editProfilePictureBase64 = null;
        $("#crop-avatar-preview, #edit-profilePicture-preview").attr("src", "");
        $("#edit-profilePicture-container, #profile-picture-preview").hide();
        toastr.error(error.message || "Failed to crop image. Please try again!");
      }
    });

  $(document)
    .off("click", "#remove-profile-picture")
    .on("click", "#remove-profile-picture", function () {
      $("#edit-profilePicture-preview").attr("src", "");
      $("#edit-profilePicture-container").hide();
      $("#edit-profilePicture").val("");
      window._editProfilePictureBase64 = null;
      selectedImageFile = null;
      window._profilePictureRemoved = true;

      if (cropper) {
        cropper.destroy();
        cropper = null;
      }

      toastr.info("Profile picture removed!");
    });

  $(form)
    .off("submit")
    .on("submit", async function (e) {
      e.preventDefault();

      const userId = $(form).data("userid");
      if (!userId) {
        toastr.error("User ID not found!");
        return;
      }

      const user = getCurrentUserInfo();
      if (!user) {
        toastr.error("User not found");
        return;
      }

      const formData = new FormData(form);
      let dateOfBirth = formData.get("dateOfBirth");

      const data = {
        fullName: formData.get("fullName")?.trim(),
        phoneNumber: formData.get("phoneNumber")?.trim(),
        address: formData.get("address")?.trim(),
        bio: formData.get("bio")?.trim(),
      };

      if (dateOfBirth && dateOfBirth.trim() !== "") {
        const today = new Date();
        if (dateOfBirth > today.toISOString().split("T")[0]) {
          toastr.error("Date of birth cannot be in the future!");
          return;
        }
        data.dateOfBirth = new Date(dateOfBirth).toISOString();
      }

      if (window._editProfilePictureBase64) {
        data.profilePicture = window._editProfilePictureBase64;
      } else if (selectedImageFile) {
        toastr.warning("Please crop your profile picture before saving!");
        return;
      } else if (window._profilePictureRemoved) {
        data.profilePicture = null;
      }

      Object.keys(data).forEach((key) => {
        if (data[key] === null || data[key] === undefined || data[key] === "") {
          delete data[key];
        }
      });

      const original = $("#editUserForm").data("original") || {};
      let changed = false;

      if (
        window._editProfilePictureBase64 ||
        selectedImageFile ||
        window._profilePictureRemoved
      ) {
        changed = true;
      }

      for (const key of Object.keys(data)) {
        let oldVal = original[key] || "";
        let newVal = data[key] || "";

        if (key === "dateOfBirth") {
          if (oldVal) oldVal = oldVal.split("T")[0];
          if (newVal) newVal = newVal.split("T")[0];
        }

        if (String(oldVal) !== String(newVal)) {
          changed = true;
          break;
        }
      }

      if (!changed) {
        toastr.warning("You have not changed any information!");
        return;
      }

      if (data.phoneNumber && !/^[0-9]{10,11}$/.test(data.phoneNumber)) {
        toastr.error("Phone number must be 10-11 digits and only numbers!");
        return;
      }

      if (!data.fullName) {
        toastr.error("Full name is required!");
        return;
      }

      const token = getToken();
      if (!token) {
        toastr.error("Authentication required!");
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/${userId}`, {
          method: "PUT",
          headers: getAuthHeaders(),
          body: JSON.stringify(data),
        });

        const responseData = await res.json();

        if (!res.ok) {
          if (responseData.errors) {
            Object.keys(responseData.errors).forEach((field) => {
              const messages = responseData.errors[field];
              if (Array.isArray(messages)) {
                messages.forEach((msg) => toastr.error(`${field}: ${msg}`));
              } else {
                toastr.error(`${field}: ${messages}`);
              }
            });
          } else if (responseData.message) {
            toastr.error(responseData.message);
          } else {
            toastr.error("Failed to update profile!");
          }
          return;
        }

        toastr.success("Profile updated successfully!");

        window._editProfilePictureBase64 = null;
        selectedImageFile = null;
        window._profilePictureRemoved = false;

        if (cropper) {
          cropper.destroy();
          cropper = null;
        }

        await loadUserInfo();

        if (window.adminAuth) {
          await window.adminAuth.updateUserProfileDisplay();
        }
      } catch (err) {
        toastr.error(err.message || "Update failed");
      }
    });

  $(document)
    .off("click", ".deactivate-account")
    .on("click", ".deactivate-account", async function (e) {
      e.preventDefault();

      if (
        !confirm(
          "Are you sure you want to deactivate your account? This cannot be undone.",
        )
      ) {
        return;
      }

      const user = getCurrentUserInfo();
      if (!user) {
        toastr.error("User not found");
        return;
      }

      try {
        const res = await fetch(`${API_BASE}/${user.id}`, {
          method: "DELETE",
          headers: getAuthHeaders(),
        });

        const data = await res.json();

        if (res.ok && data.success) {
          toastr.success("Account deactivated. Logging out...");
          localStorage.removeItem("authToken");
          setTimeout(() => {
            window.location.href = "/auth/login.html";
          }, 1500);
        } else {
          toastr.error(data.message || "Deactivation failed");
        }
      } catch (err) {
        toastr.error(err.message || "Deactivation failed");
      }
    });

  // --- CropperJS integration ---
  function initializeCropper(imageElement, imageUrl) {
    $(".drag-drop-zone").addClass("hidden");
    currentZoom = 1;
    cropperReady = false;
    initialCropBoxWidth = null;
    cropper = new Cropper(imageElement, {
      aspectRatio: 1,
      viewMode: 1,
      dragMode: "crop",
      autoCropArea: 0.8,
      background: false,
      responsive: true,
      movable: false,
      rotatable: true,
      scalable: false,
      zoomable: true,
      zoomOnWheel: true,
      wheelZoomRatio: 0.1,
      cropBoxMovable: true,
      cropBoxResizable: true,
      toggleDragModeOnDblclick: false,
      ready: function () {
        let triedResize = false;
        let tryCount = 0;
        function waitForCropBox() {
          tryCount++;
          let cropBox, imageData;
          try {
            cropBox = cropper.getCropBoxData();
            imageData = cropper.getImageData();
          } catch (e) {
            toastr.error("Failed to initialize cropper. Please try another image.");
            $("#cropImageModal").modal("hide");
            return;
          }
          if (!imageData || !cropBox) return setTimeout(waitForCropBox, 30);
          // Set cropBox mặc định nhỏ hơn ảnh (80%) và ở chính giữa nếu lần đầu
          if (tryCount === 1) {
            const boxSize = Math.floor(Math.min(imageData.naturalWidth, imageData.naturalHeight) * 0.8);
            cropper.setCropBoxData({
              width: boxSize,
              height: boxSize,
              left: imageData.left + (imageData.naturalWidth - boxSize) / 2,
              top: imageData.top + (imageData.naturalHeight - boxSize) / 2
            });
            setTimeout(waitForCropBox, 30);
            return;
          }
          // Nếu cropBox vượt biên, tự động điều chỉnh về vùng hợp lệ
          let adjusted = false;
          let newLeft = cropBox.left;
          let newTop = cropBox.top;
          if (cropBox.left < imageData.left) {
            newLeft = imageData.left;
            adjusted = true;
          }
          if (cropBox.top < imageData.top) {
            newTop = imageData.top;
            adjusted = true;
          }
          if (cropBox.left + cropBox.width > imageData.left + imageData.naturalWidth) {
            newLeft = imageData.left + imageData.naturalWidth - cropBox.width;
            adjusted = true;
          }
          if (cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight) {
            newTop = imageData.top + imageData.naturalHeight - cropBox.height;
            adjusted = true;
          }
          if (adjusted) {
            cropper.setCropBoxData({
              width: cropBox.width,
              height: cropBox.height,
              left: newLeft,
              top: newTop
            });
            setTimeout(waitForCropBox, 30);
            return;
          }
          // Nếu ảnh quá nhỏ (bé hơn 100x100), báo lỗi và không cho crop
          if (imageData.naturalWidth < 100 || imageData.naturalHeight < 100) {
            toastr.error("Image is too small. Please use an image at least 100x100px.");
            $("#cropImageModal").modal("hide");
            return;
          }
          // Nếu thử quá 10 lần mà cropBox vẫn không hợp lệ, báo lỗi
          if (tryCount > 10 && (!cropBox || cropBox.width <= 0 || cropBox.height <= 0)) {
            toastr.error("Failed to initialize cropper. Please try another image.");
            $("#cropImageModal").modal("hide");
            return;
          }
          // Lưu lại cropBox.width ban đầu để tính zoom đúng chuẩn
          if (!initialCropBoxWidth) initialCropBoxWidth = cropBox.width;
          // maxZoom là tỉ lệ nhỏ nhất để cropBox vừa khít ảnh
          let maxZoom = Math.min(
            Math.floor((initialCropBoxWidth / 10) * 100) / 100, // không cho nhỏ hơn 10px
            imageData.naturalWidth / initialCropBoxWidth,
            3
          );
          maxZoom = Math.max(1, maxZoom);
          cropperReady = true;
          updateZoomSlider(maxZoom);
          updateCircleOverlay();
          updateAvatarPreview();
          // Disable slider nếu maxZoom=1
          const zoomSlider = document.getElementById("zoom-slider");
          if (zoomSlider) {
            zoomSlider.disabled = maxZoom === 1;
            zoomSlider.max = maxZoom.toFixed(2);
          }
          $("#zoom-in-btn, #zoom-out-btn").prop("disabled", maxZoom === 1);
        }
        waitForCropBox();
        const zoomSlider = document.getElementById("zoom-slider");
        if (zoomSlider) {
          zoomSlider.min = 1;
          zoomSlider.max = 3;
          zoomSlider.step = 0.01;
          zoomSlider.value = 1;
          zoomSlider.oninput = function () {
            let val = parseFloat(this.value);
            if (val < 1) val = 1;
            if (val > parseFloat(this.max)) val = parseFloat(this.max);
            if (val === currentZoom) return;
            this.value = val;
            currentZoom = val;
            // Tính lại cropBox width theo initialCropBoxWidth
            if (initialCropBoxWidth) {
              const newWidth = initialCropBoxWidth / currentZoom;
              const cropBox = cropper.getCropBoxData();
              cropper.setCropBoxData({
                width: newWidth,
                height: newWidth,
                left: cropBox.left,
                top: cropBox.top
              });
            } else {
              cropper.zoomTo(currentZoom);
            }
            updateZoomDisplay(currentZoom);
          };
        }
        updateZoomSlider();
        updateCircleOverlay();
        updateAvatarPreview();
        $("#zoom-in-btn")
          .off("click")
          .on("click", function () {
            const zoomSlider = document.getElementById("zoom-slider");
            let maxZoom = zoomSlider ? parseFloat(zoomSlider.max) : 3;
            if (currentZoom >= maxZoom) return;
            let newZoom = Math.min(currentZoom + 0.1, maxZoom);
            if (newZoom === currentZoom) return;
            currentZoom = newZoom;
            if (initialCropBoxWidth) {
              const newWidth = initialCropBoxWidth / currentZoom;
              const cropBox = cropper.getCropBoxData();
              cropper.setCropBoxData({
                width: newWidth,
                height: newWidth,
                left: cropBox.left,
                top: cropBox.top
              });
            } else {
              cropper.zoomTo(currentZoom);
            }
            updateZoomSlider(maxZoom);
            updateZoomDisplay(currentZoom);
          });
        $("#zoom-out-btn")
          .off("click")
          .on("click", function () {
            if (currentZoom <= 1) return;
            let newZoom = Math.max(currentZoom - 0.1, 1);
            if (newZoom === currentZoom) return;
            currentZoom = newZoom;
            if (initialCropBoxWidth) {
              const newWidth = initialCropBoxWidth / currentZoom;
              const cropBox = cropper.getCropBoxData();
              cropper.setCropBoxData({
                width: newWidth,
                height: newWidth,
                left: cropBox.left,
                top: cropBox.top
              });
            } else {
              cropper.zoomTo(currentZoom);
            }
            updateZoomSlider();
            updateZoomDisplay(currentZoom);
          });
        $("#rotate-left-btn")
          .off("click")
          .on("click", function () {
            cropper.rotate(-90);
          });
        $("#rotate-right-btn")
          .off("click")
          .on("click", function () {
            cropper.rotate(90);
          });
        $("#reset-btn")
          .off("click")
          .on("click", function () {
            cropper.reset();
            currentZoom = 1;
            updateZoomSlider();
            updateZoomDisplay(currentZoom);
          });
      },
      zoom: function (event) {
        let ratio = event.detail.ratio;
        const zoomSlider = document.getElementById("zoom-slider");
        let maxZoom = zoomSlider ? parseFloat(zoomSlider.max) : 3;
        if (ratio < 1) {
          event.preventDefault();
          currentZoom = 1;
        } else if (ratio > maxZoom) {
          event.preventDefault();
          currentZoom = maxZoom;
        } else {
          currentZoom = ratio;
        }
        if (zoomSlider) {
          zoomSlider.value = currentZoom;
          updateZoomDisplay(currentZoom);
        }
        updateCircleOverlay();
      },
      crop: function (event) {
        updateCircleOverlay();
        if (cropperReady) updateAvatarPreview();
        updateZoomSlider();
      },
      cropmove: function () {
        updateCircleOverlay();
        if (cropperReady) updateAvatarPreview();
      },
      error: function () {
        toastr.error("Failed to load image. Please try another image.");
        $("#cropImageModal").modal("hide");
      }
    });
    window._cropper = cropper;
  }

  function updateZoomSlider(maxZoom) {
    const zoomSlider = document.getElementById("zoom-slider");
    if (zoomSlider) {
      if (maxZoom) zoomSlider.max = maxZoom;
      zoomSlider.value = currentZoom;
      updateZoomDisplay(currentZoom);
      const minLabel = zoomSlider.parentElement?.previousElementSibling;
      const maxLabel = document.getElementById("zoom-max-label");
      if (minLabel) minLabel.textContent = "100%";
      if (maxLabel && maxZoom) maxLabel.textContent = `${Math.round(maxZoom * 100)}%`;
    }
  }

  function ensureCropBoxInBounds() {
    if (!cropper) return;
    const cropBox = cropper.getCropBoxData();
    const imageData = cropper.getImageData();
    let adjusted = false;
    let newLeft = cropBox.left;
    let newTop = cropBox.top;
    if (cropBox.left < imageData.left) {
      newLeft = imageData.left;
      adjusted = true;
    }
    if (cropBox.top < imageData.top) {
      newTop = imageData.top;
      adjusted = true;
    }
    if (cropBox.left + cropBox.width > imageData.left + imageData.naturalWidth) {
      newLeft = imageData.left + imageData.naturalWidth - cropBox.width;
      adjusted = true;
    }
    if (cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight) {
      newTop = imageData.top + imageData.naturalHeight - cropBox.height;
      adjusted = true;
    }
    if (adjusted) {
      cropper.setCropBoxData({
        width: cropBox.width,
        height: cropBox.height,
        left: newLeft,
        top: newTop
      });
    }
  }

  function updateZoomDisplay(zoom) {
    const percentage = Math.round((zoom || 1) * 100);
    const zoomDisplay = document.querySelector(".zoom-percentage");
    if (zoomDisplay) {
      zoomDisplay.textContent = `${percentage}%`;
    }
  }

  function updateCircleOverlay() {
    if (!cropper) return;
    const overlay = document.querySelector(".crop-circle-overlay");
    if (!overlay) return;
    const cropBox = cropper.getCropBoxData();
    overlay.style.width = `${cropBox.width}px`;
    overlay.style.height = `${cropBox.height}px`;
    overlay.style.left = `${cropBox.left}px`;
    overlay.style.top = `${cropBox.top}px`;
    overlay.style.display = "block";
  }

  function updateAvatarPreview() {
    if (!cropper || !cropperReady) return;
    let cropBox;
    try {
      cropBox = cropper.getCropBoxData();
      const imageData = cropper.getImageData();
      // Đảm bảo cropBox nằm trong vùng ảnh
      if (
        cropBox.left < imageData.left ||
        cropBox.top < imageData.top ||
        cropBox.left + cropBox.width > imageData.left + imageData.naturalWidth ||
        cropBox.top + cropBox.height > imageData.top + imageData.naturalHeight
      ) {
        ensureCropBoxInBounds();
        cropBox = cropper.getCropBoxData();
      }
      if (!cropBox || cropBox.width <= 0 || cropBox.height <= 0) return;
      if (imageData && (cropBox.width > imageData.naturalWidth || cropBox.height > imageData.naturalHeight)) {
        const preview = document.getElementById("crop-avatar-preview");
        if (preview) {
          preview.src = "";
          preview.style.display = "none";
        }
        return;
      }
      const canvas = cropper.getCroppedCanvas({
        width: 200,
        height: 200,
        imageSmoothingQuality: "high",
        fillColor: "#fff",
      });
      if (!canvas) return;
      const preview = document.getElementById("crop-avatar-preview");
      if (canvas && preview) {
        const circleCanvas = document.createElement("canvas");
        circleCanvas.width = 200;
        circleCanvas.height = 200;
        const ctx = circleCanvas.getContext("2d");
        ctx.save();
        ctx.beginPath();
        ctx.arc(100, 100, 100, 0, 2 * Math.PI);
        ctx.closePath();
        ctx.clip();
        ctx.drawImage(canvas, 0, 0, 200, 200);
        ctx.restore();
        preview.src = circleCanvas.toDataURL("image/png");
        preview.style.display = "block";
      } else if (preview) {
        preview.src = "";
        preview.style.display = "none";
      }
    } catch (error) {
      const preview = document.getElementById("crop-avatar-preview");
      if (preview) {
        preview.src = "";
        preview.style.display = "none";
      }
    }
  }

  // Drag & Drop functionality
  $(document).on("dragover", ".drag-drop-zone", function (e) {
    e.preventDefault();
    $(this).addClass("dragover");
  });

  $(document).on("dragleave", ".drag-drop-zone", function (e) {
    e.preventDefault();
    $(this).removeClass("dragover");
  });

  $(document).on("drop", ".drag-drop-zone", function (e) {
    e.preventDefault();
    $(this).removeClass("dragover");

    const files = e.originalEvent.dataTransfer.files;
    if (files.length > 0) {
      const file = files[0];
      if (file.type.startsWith("image/")) {
        handleImageFile(file);
      } else {
        toastr.error("Please select a valid image file!");
      }
    }
  });

  $(document).on("click", ".drag-drop-zone", function () {
    $("#edit-profilePicture, #profile-picture-input").click();
  });

  function handleImageFile(file) {
    if (file.size > 5 * 1024 * 1024) {
      toastr.error("Image size must be less than 5MB!");
      return;
    }
    if (file.size < 10 * 1024) {
      toastr.warning(
        "Image size is very small. For better quality, use an image larger than 10KB.",
      );
    }

    selectedImageFile = file;
    const reader = new FileReader();
    reader.onload = function (ev) {
      const $img = $("#cropper-image");
      if (cropper) {
        cropper.destroy();
        cropper = null;
      }
      $img.attr("src", ev.target.result);
      $img.off("load").on("load", function () {
        initializeCropper($img[0], ev.target.result);
      });
      if ($img[0].complete) {
        $img.trigger("load");
      }
    };
    reader.readAsDataURL(file);
  }

  function openCropperModal(imageUrl) {
    const $modal = $("#cropImageModal");
    const $img = $("#cropper-image");
    if (cropper) {
      cropper.destroy();
      cropper = null;
    }
    $img.hide();
    $img.attr("src", imageUrl);
    $modal.modal("show");
    $img.off("load").on("load", function () {
      $img.show();
      initializeCropper($img[0], imageUrl);
    });
  }

  await loadUserInfo();

  // Ensure cropper is destroyed when modal is closed (prevents errors and double-initialization)
  $("#cropImageModal").on("hidden.bs.modal", function () {
    if (cropper) {
      cropper.destroy();
      cropper = null;
    }
    if (!window._editProfilePictureBase64) {
      $("#edit-profilePicture, #profile-picture-input").val("");
      selectedImageFile = null;
    }
    // Show drag drop zone again
    $(".drag-drop-zone").removeClass("hidden");
  });
});
