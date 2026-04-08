$(document).ready(function () {
    // AJAX Form Submission Handler
    $(document).on('submit', 'form[data-ajax="true"]', function (e) {
        e.preventDefault();
        var $form = $(this);
        var url = $form.attr('action');
        var method = $form.attr('method') || 'POST';
        
        // Ensure SID is in URL for AJAX context if not present
        var currentPath = window.location.pathname.split('/');
        var sid = currentPath[1];
        if (sid.startsWith('s') && !url.includes('/' + sid + '/')) {
            url = '/' + sid + (url.startsWith('/') ? '' : '/') + url;
        }

        var formData = new FormData(this);

        // Show loading state
        var $submitBtn = $form.find('button[type="submit"]');
        var originalBtnHtml = $submitBtn.html();
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...');

        $.ajax({
            url: url,
            type: method,
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: response.message || 'Action completed successfully.',
                        icon: 'success',
                        confirmButtonColor: '#0d6efd'
                    }).then((result) => {
                        if (response.redirectUrl) {
                            window.location.href = response.redirectUrl;
                        } else if (response.reload) {
                            window.location.reload();
                        }
                    });
                } else {
                    Swal.fire({
                        title: 'Notice',
                        text: response.message || 'There was an issue processing your request.',
                        icon: 'warning',
                        confirmButtonColor: '#0d6efd'
                    });
                }
            },
            error: function (xhr) {
                var errorMessage = 'An unexpected error occurred.';
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMessage = xhr.responseJSON.message;
                } else if (xhr.status === 400) {
                    errorMessage = 'Please check your inputs and try again.';
                }

                Swal.fire({
                    title: 'Error!',
                    text: errorMessage,
                    icon: 'error',
                    confirmButtonColor: '#0d6efd'
                });
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnHtml);
            }
        });
    });

    // Auto-dismiss alerts after 5 seconds
    setTimeout(function () {
        $(".alert-dismissible").fadeTo(500, 0).slideUp(500, function () {
            $(this).remove();
        });
    }, 5000);

    // Global SID Persistence for Links
    $(document).on('click', 'a[href]', function (e) {
        var href = $(this).attr('href');
        var currentPath = window.location.pathname.split('/');
        var sid = currentPath[1];

        if (sid.startsWith('s') && href && !href.startsWith('http') && !href.startsWith('#') && !href.startsWith('javascript:')) {
            if (!href.includes('/' + sid + '/')) {
                e.preventDefault();
                var newHref = '/' + sid + (href.startsWith('/') ? '' : '/') + href;
                window.location.href = newHref;
            }
        }
    });
});
