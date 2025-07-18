$(document).ready(function () {
  if (!window.adminAuth) {
    return;
  }
  
  const $form = $('#uploadFileForm');
  const $fileInput = $('#fileInput');
  const $filePreviewContainer = $('#filePreviewContainer');
  const $filePreview = $('#filePreview');
  const $uploadBtn = $('#uploadBtn');
  const $desc = $('#fileDescription');

  function safeToastr(type, msg) {
    if (typeof window.i18next !== "undefined" && typeof window.i18next.t === "function") {
      msg = window.i18next.t(msg);
    }
    if (typeof toastr !== "undefined") {
      toastr.clear();
      toastr[type](msg);
    }
  }

  $fileInput.on('change', function () {
    const file = this.files[0];
    if (!file) {
      $filePreviewContainer.hide();
      $filePreview.html('');
      return;
    }
    let previewHtml = '';
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = function (e) {
        previewHtml = `<img src="${e.target.result}" alt="preview" style="max-width:100%;max-height:200px;" />`;
        $filePreview.html(previewHtml);
        $filePreviewContainer.show();
      };
      reader.readAsDataURL(file);
    } else if (file.type.startsWith('text/')) {
      const reader = new FileReader();
      reader.onload = function (e) {
        previewHtml = `<pre style='max-height:200px;overflow:auto;'>${$('<div>').text(e.target.result).html()}</pre>`;
        $filePreview.html(previewHtml);
        $filePreviewContainer.show();
      };
      reader.readAsText(file);
    } else if (file.type === 'application/pdf') {
      previewHtml = `<span class='text-info'><i class='ti ti-file-text me-2'></i>PDF: ${file.name}</span>`;
      $filePreview.html(previewHtml);
      $filePreviewContainer.show();
    } else {
      previewHtml = `<span class='text-secondary'><i class='ti ti-file me-2'></i>${file.name}</span>`;
      $filePreview.html(previewHtml);
      $filePreviewContainer.show();
    }
  });

  $form.on('submit', async function (e) {
    e.preventDefault();
    const file = $fileInput[0].files[0];
    if (!file) {
      safeToastr('error', 'chooseFile');
      return;
    }
    if (file.size > 100 * 1024 * 1024) {
      safeToastr('error', 'maxFileSize');
      return;
    }
    $uploadBtn.prop('disabled', true).text(window.i18next.t('uploading'));
    const formData = new FormData();
    formData.append('Files', file);
    formData.append('description', $desc.val() || '');
    try {
      const token = localStorage.getItem("authToken");
      const res = await fetch('http://localhost:5050/api/File/upload', {
        method: 'POST',
        body: formData,
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      if (!res.ok) {
        const err = await res.json().catch(() => ({}));
        throw new Error(err.message || 'Upload failed');
      }
      sessionStorage.setItem('fileUploadSuccess', '1');
      $form[0].reset();
      $filePreviewContainer.hide();
      $filePreview.html('');
      safeToastr('success', 'uploadSuccess');
      window.location.href = 'files.html';
    } catch (err) {
      safeToastr('error', 'uploadFailed') + ': ' + (err.message || '');
    } finally {
      $uploadBtn.prop('disabled', false).text(window.i18next.t('upload'));
    }
  });
}); 