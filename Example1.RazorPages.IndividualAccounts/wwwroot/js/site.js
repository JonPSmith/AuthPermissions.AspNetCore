// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//Used in Edit / Create of a Role
function TogglePermissionSelect(button, idOfInput, selectLocalized, selectedLocalized) {
    if ($(button).text().trim() === selectedLocalized) {
        $(button).text(selectLocalized);
        $(button).removeClass('btn-primary');
        $(button).addClass('btn-secondary');
        $('#' + idOfInput).val(false);
    } else {
        $(button).text(selectedLocalized);
        $(button).removeClass('btn-secondary');
        $(button).addClass('btn-primary');
        $('#' + idOfInput).val(true);
    }
}
