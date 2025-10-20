document.addEventListener("DOMContentLoaded", function () {
    const loadingIndicator = document.getElementById("loadingIndicator");

    function showLoading() {
        loadingIndicator.style.display = "block";
    }

    function hideLoading() {
        loadingIndicator.style.display = "none";
    }

    // Show loading when navigating to a new page
    window.addEventListener("beforeunload", function () {
        showLoading();
    });

    // Attach event listeners to all forms for submission
    document.querySelectorAll("form").forEach(form => {
        form.addEventListener("submit", function () {
            showLoading();
        });
    });

    // Handle AJAX requests
    $(document).ajaxStart(function () {
        showLoading();
    }).ajaxStop(function () {
        hideLoading();
    });

    // Hide loader after the page fully loads
    window.onload = function () {
        hideLoading();
    };
});
