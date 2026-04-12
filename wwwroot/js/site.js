$(document).ready(function () {
    // --- UI STATE MANAGEMENT (Theme & Sidebar) ---
    
    // Initialize Theme
    const savedTheme = localStorage.getItem('ims-theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);

    // Initialize Sidebar State
    const savedSidebar = localStorage.getItem('ims-sidebar-state');
    if (savedSidebar === 'collapsed') {
        $('#sidebar').addClass('collapsed');
    }

    // Toggle Theme Function
    window.toggleTheme = function() {
        const currentTheme = document.documentElement.getAttribute('data-theme');
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        
        document.documentElement.setAttribute('data-theme', newTheme);
        localStorage.setItem('ims-theme', newTheme);
        updateThemeIcon(newTheme);
    };

    // Toggle Sidebar Function
    window.toggleSidebar = function() {
        const $sidebar = $('#sidebar');
        if (window.innerWidth <= 991.98) {
            $sidebar.toggleClass('mobile-open');
        } else {
            $sidebar.toggleClass('collapsed');
            const state = $sidebar.hasClass('collapsed') ? 'collapsed' : 'expanded';
            localStorage.setItem('ims-sidebar-state', state);
        }
    };

    function updateThemeIcon(theme) {
        const $icon = $('#theme-icon');
        if (theme === 'dark') {
            $icon.removeClass('bi-moon-stars-fill').addClass('bi-sun-fill');
        } else {
            $icon.removeClass('bi-sun-fill').addClass('bi-moon-stars-fill');
        }
    }

    // --- AJAX FORM HANDLER ---
    $(document).on('submit', 'form[data-ajax="true"]', function (e) {
        e.preventDefault();
        var $form = $(this);
        var url = $form.attr('action');
        var method = $form.attr('method') || 'POST';
        
        // Ensure SID is in URL for AJAX context
        var currentPath = window.location.pathname.split('/');
        var sid = currentPath[1];
        if (sid.startsWith('s') && !url.includes('/' + sid + '/')) {
            url = '/' + sid + (url.startsWith('/') ? '' : '/') + url;
        }

        var formData = new FormData(this);
        
        // BUG FIX: FormData(this) doesn't include the clicked submit button's name/value.
        // We capture the button that triggered the submit and append it.
        var $submitter = $(e.originalEvent.submitter);
        if ($submitter.length && $submitter.attr('name')) {
            formData.append($submitter.attr('name'), $submitter.val());
        }

        // Show loading state (Standardized button loading)
        var $submitBtn = $submitter.length ? $submitter : $form.find('button[type="submit"]');
        var originalBtnHtml = $submitBtn.html();
        $submitBtn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-2"></span>Processing...');

        $.ajax({
            url: url,
            type: method,
            data: formData,
            processData: false,
            contentType: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: response.message || 'Action completed successfully.',
                        icon: 'success',
                        confirmButtonColor: '#0d6efd',
                        customClass: { popup: 'ims-card' }
                    }).then(() => {
                        if (response.redirectUrl) window.location.href = response.redirectUrl;
                        else if (response.reload) window.location.reload();
                    });
                } else {
                    Swal.fire({
                        title: 'Notice',
                        text: response.message || 'Issue processing request.',
                        icon: 'warning',
                        confirmButtonColor: '#0d6efd',
                        customClass: { popup: 'ims-card' }
                    });
                }
            },
            error: function (xhr) {
                var msg = (xhr.responseJSON && xhr.responseJSON.message) ? xhr.responseJSON.message : 'An unexpected error occurred.';
                Swal.fire({
                    title: 'Error!',
                    text: msg,
                    icon: 'error',
                    confirmButtonColor: '#0d6efd',
                    customClass: { popup: 'ims-card' }
                });
            },
            complete: function () {
                $submitBtn.prop('disabled', false).html(originalBtnHtml);
            }
        });
    });

    // --- GLOBAL SID PERSISTENCE ---
    $(document).on('click', 'a[href]', function (e) {
        var href = $(this).attr('href');
        var currentPath = window.location.pathname.split('/');
        var sid = currentPath[1];

        if (sid.startsWith('s') && href && !href.startsWith('http') && !href.startsWith('#') && !href.startsWith('javascript:')) {
            if (!href.includes('/' + sid + '/')) {
                e.preventDefault();
                window.location.href = '/' + sid + (href.startsWith('/') ? '' : '/') + href;
            }
        }
    });

    // Auto-dismiss alerts
    setTimeout(() => {
        $(".alert-dismissible").fadeTo(500, 0).slideUp(500, function () { $(this).remove(); });
    }, 5000);
});
