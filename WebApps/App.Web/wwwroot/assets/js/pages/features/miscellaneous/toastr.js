"use strict";

// Class definition
var KTToastrDemo = function() {
    
    // Private functions
    var demos = function() {
        // Configure toastr options globally
        toastr.options = {
            "closeButton": true,
            "debug": false,
            "newestOnTop": true,
            "progressBar": true,
            "positionClass": "toast-top-right",
            "preventDuplicates": false,
            "onclick": null,
            "showDuration": "300",
            "hideDuration": "1000",
            "timeOut": "5000",
            "extendedTimeOut": "1000",
            "showEasing": "swing",
            "hideEasing": "linear",
            "showMethod": "fadeIn",
            "hideMethod": "fadeOut",
            "toastClass": "toastr-custom",
            "iconClasses": {
                error: 'toast-error',
                info: 'toast-info',
                success: 'toast-success',
                warning: 'toast-warning'
            }
        };
    }

    return {
        // public functions
        init: function() {
            demos(); 
        }
    };
}();

jQuery(document).ready(function() {    
    KTToastrDemo.init();
});

// Custom CSS to fix toastr colors
var style = document.createElement('style');
style.innerHTML = `
    /* Toastr Custom Styles - Fix for white text issue */
    .toastr-custom {
        opacity: 1 !important;
    }
    
    #toast-container > div {
        opacity: 1 !important;
        box-shadow: 0 0 12px rgba(0,0,0,0.2);
        padding: 15px 15px 15px 50px;
        width: 300px;
    }
    
    /* Success Toast */
    #toast-container > .toast-success {
        background-color: #28a745 !important;
        color: #ffffff !important;
        background-image: none !important;
    }
    
    #toast-container > .toast-success .toast-title {
        color: #ffffff !important;
        font-weight: 600;
    }
    
    #toast-container > .toast-success .toast-message {
        color: #ffffff !important;
    }
    
    /* Error Toast */
    #toast-container > .toast-error {
        background-color: #dc3545 !important;
        color: #ffffff !important;
        background-image: none !important;
    }
    
    #toast-container > .toast-error .toast-title {
        color: #ffffff !important;
        font-weight: 600;
    }
    
    #toast-container > .toast-error .toast-message {
        color: #ffffff !important;
    }
    
    /* Info Toast */
    #toast-container > .toast-info {
        background-color: #17a2b8 !important;
        color: #ffffff !important;
        background-image: none !important;
    }
    
    #toast-container > .toast-info .toast-title {
        color: #ffffff !important;
        font-weight: 600;
    }
    
    #toast-container > .toast-info .toast-message {
        color: #ffffff !important;
    }
    
    /* Warning Toast */
    #toast-container > .toast-warning {
        background-color: #ffc107 !important;
        color: #000000 !important;
        background-image: none !important;
    }
    
    #toast-container > .toast-warning .toast-title {
        color: #000000 !important;
        font-weight: 600;
    }
    
    #toast-container > .toast-warning .toast-message {
        color: #000000 !important;
    }
    
    /* Close button styling */
    #toast-container > div .toast-close-button {
        color: #ffffff !important;
        opacity: 0.8;
        font-size: 18px;
        font-weight: bold;
        text-shadow: none;
    }
    
    #toast-container > .toast-warning .toast-close-button {
        color: #000000 !important;
    }
    
    #toast-container > div .toast-close-button:hover {
        opacity: 1;
    }
    
    /* Progress bar styling */
    #toast-container > div .toast-progress {
        opacity: 0.6;
        background-color: rgba(255, 255, 255, 0.7);
    }
    
    #toast-container > .toast-warning .toast-progress {
        background-color: rgba(0, 0, 0, 0.3);
    }
    
    /* Add icons using Font Awesome or custom */
    #toast-container > .toast-success:before {
        content: "✓";
        font-size: 24px;
        font-weight: bold;
        color: #ffffff;
        position: absolute;
        left: 15px;
        top: 50%;
        transform: translateY(-50%);
    }
    
    #toast-container > .toast-error:before {
        content: "✕";
        font-size: 24px;
        font-weight: bold;
        color: #ffffff;
        position: absolute;
        left: 15px;
        top: 50%;
        transform: translateY(-50%);
    }
    
    #toast-container > .toast-info:before {
        content: "ℹ";
        font-size: 24px;
        font-weight: bold;
        color: #ffffff;
        position: absolute;
        left: 15px;
        top: 50%;
        transform: translateY(-50%);
    }
    
    #toast-container > .toast-warning:before {
        content: "⚠";
        font-size: 24px;
        font-weight: bold;
        color: #000000;
        position: absolute;
        left: 15px;
        top: 50%;
        transform: translateY(-50%);
    }
    
    /* RTL Support */
    [dir="rtl"] #toast-container > div {
        padding: 15px 50px 15px 15px;
    }
    
    [dir="rtl"] #toast-container > .toast-success:before,
    [dir="rtl"] #toast-container > .toast-error:before,
    [dir="rtl"] #toast-container > .toast-info:before,
    [dir="rtl"] #toast-container > .toast-warning:before {
        left: auto;
        right: 15px;
    }
    
    [dir="rtl"] #toast-container > div .toast-close-button {
        right: auto;
        left: -0.3em;
    }
`;
document.head.appendChild(style);
