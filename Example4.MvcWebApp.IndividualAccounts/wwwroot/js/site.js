// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

//------------------------------------------------------------
//RoleCreateEdit  code

function submitRoleWithPermissions(url) {
    var forms = $('#permission-selection').find('form');
    var params = {
        'RoleName': $('#role-name').val(),
        'Description': $('#role-description').val(),
        '__RequestVerificationToken': $('input:hidden[name="__RequestVerificationToken"]').val()
    };

    var paramCount = 0;
    for (var i = 0; i < forms.length; i++) {
        if ($(forms[i]).find('button').text().trim() === 'Selected')
            params['permissions[' + paramCount++ + ']'] = $(forms[i]).attr('id');
    };

    postParamsAsFormSubmit(url, params);
}

function TogglePermissionSelect(button) {
    if ($(button).text() === 'Selected') {
        $(button).text('Select');
        $(button).removeClass('btn-primary');
        $(button).addClass('btn-secondary');
    } else {
        $(button).text('Selected');
        $(button).removeClass('btn-secondary');
        $(button).addClass('btn-primary');
    }
}

//---------------------------------------------------------------------
// General code used to send back data as if from a form

//thanks to https://stackoverflow.com/questions/133925/javascript-post-request-like-a-form-submit
// Post to the provided URL with the specified parameters.
function postParamsAsFormSubmit(path, parameters) {
    var form = $('<form></form>');

    form.attr("method", "post");
    form.attr("action", path);

    $.each(parameters,
        function (key, value) {
            var field = $('<input></input>');

            field.attr("type", "hidden");
            field.attr("name", key);
            field.attr("value", value);

            form.append(field);
        });

    // The form needs to be a part of the document in
    // order for us to be able to submit it.
    $(document.body).append(form);
    form.submit();
}
