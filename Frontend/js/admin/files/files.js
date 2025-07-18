function getFileExtension(filename) {
  if (!filename) return "Unknown";
  const ext = filename.split(".").pop();
  if (!ext || ext === filename) return "Unknown";
  return ext.toUpperCase();
}

function formatFileSize(size) {
  if (size >= 1024 * 1024) return (size / (1024 * 1024)).toFixed(2) + " MB";
  if (size >= 1024) return (size / 1024).toFixed(2) + " KB";
  return size + " B";
}

async function fetchAndRenderFiles() {
  try {
    const token = localStorage.getItem("authToken");
    const res = await fetch("http://localhost:5050/api/File/list", {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });
    if (!res.ok) throw new Error("Failed to fetch file list");
    const data = await res.json();
    window.fileListData = Array.isArray(data) ? data : [];
    window.loadFilesTable();
  } catch (err) {
    window.fileListData = [];
    window.loadFilesTable();
    if (typeof toastr !== "undefined")
      toastr.error(err.message || "Failed to load files");
  }
}

function openDeleteFileModal(file) {
  document.querySelector(".delete-file-name").textContent =
    file.fileName || "-";
  document.querySelector(".delete-file-size").textContent =
    typeof file.fileSize === "number" ? formatFileSize(file.fileSize) : "-";
  document.querySelector(".delete-file-type").textContent = getFileExtension(
    file.fileName,
  );
  document.querySelector(".delete-file-created").textContent = file.uploadedAt
    ? new Date(file.uploadedAt).toLocaleString("en-GB")
    : "-";
  $("#deleteFileModal").modal("show");
  const confirmBtn = document.getElementById("confirmDeleteFile");
  confirmBtn.onclick = null;
  confirmBtn.onclick = async function () {
    try {
      const token = localStorage.getItem("authToken");
      const res = await fetch(
        `http://localhost:5050/api/File/delete/${encodeURIComponent(file.fileName)}`,
        {
          method: "DELETE",
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        },
      );
      if (!res.ok) throw new Error("Failed to delete file");
      if (typeof toastr !== "undefined")
        toastr.success("File deleted successfully");
      $("#deleteFileModal").modal("hide");
      fetchAndRenderFiles();
    } catch (err) {
      if (typeof toastr !== "undefined")
        toastr.error(err.message || "Failed to delete file");
    }
  };
}

document.addEventListener("DOMContentLoaded", fetchAndRenderFiles);

window.loadFilesTable = async function () {
  const files = window.fileListData || [];
  const tbody = document.querySelector(".datatables-files tbody");
  if (!tbody) return;
  if (!files.length) {
    tbody.innerHTML = `<tr><td colspan="5">No files found</td></tr>`;
    return;
  }
  tbody.innerHTML = files
    .map(
      (file, idx) => `
    <tr class="file-row" data-file-idx="${idx}">
      <td>${file.fileName || "-"}</td>
      <td>${typeof file.fileSize === "number" ? formatFileSize(file.fileSize) : "-"}</td>
      <td>${getFileExtension(file.fileName)}</td>
      <td>${file.uploadedAt ? new Date(file.uploadedAt).toLocaleString("en-GB") : "-"}</td>
      <td>
        <a href="javascript:;" class="text-body btn-view-file" title="View" data-bs-toggle="tooltip"><i class="ti ti-eye text-primary me-1"></i></a>
        <a href="javascript:;" class="text-body btn-delete-file" title="Delete" data-bs-toggle="tooltip"><i class="ti ti-trash text-danger me-1"></i></a>
      </td>
    </tr>
  `,
    )
    .join("");
  tbody.querySelectorAll("tr.file-row").forEach((row, idx) => {
    row.querySelectorAll("td").forEach((cell) => {
      cell.style.cursor = "pointer";
      cell.onclick = function (e) {
        if (e.target.closest(".btn-delete-file")) {
          openDeleteFileModal(files[idx]);
          return;
        }
        showFileDetailModal(files[idx]);
      };
    });
    row.querySelector(".btn-view-file").onclick = function (e) {
      e.preventDefault();
      showFileDetailModal(files[idx]);
    };
  });
};

window.showFileDetailModal = function (file) {
  document.querySelector(".file-name").textContent = file.fileName || "-";
  document.querySelector(".file-size").textContent =
    typeof file.fileSize === "number" ? formatFileSize(file.fileSize) : "-";
  document.querySelector(".file-type").textContent = getFileExtension(
    file.fileName,
  );
  document.querySelector(".file-created").textContent = file.uploadedAt
    ? new Date(file.uploadedAt).toLocaleString("en-GB")
    : "-";

  const descElem = document.querySelector(".file-description");
  if (descElem) {
    if (file.description && file.description.trim() !== "") {
      descElem.textContent = file.description;
      descElem.parentElement.style.display = "block";
    } else {
      descElem.textContent = "-";
      descElem.parentElement.style.display = "none";
    }
  }

  const downloadBtn = document.querySelector(".btn-download-file");
  downloadBtn.onclick = function () {
    if (!file.fileUrl || !file.fileName) return;
    const link = document.createElement("a");
    link.href = file.fileUrl;
    link.download = file.fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const deleteBtn = document.querySelector("#viewFileModal .btn-delete-file");
  deleteBtn.onclick = function () {
    $("#viewFileModal").off("hidden.bs.modal");
    $("#viewFileModal").one("hidden.bs.modal", function () {
      openDeleteFileModal(file);
    });
    $("#viewFileModal").modal("hide");
  };

  $("#viewFileModal").modal("show");
};
