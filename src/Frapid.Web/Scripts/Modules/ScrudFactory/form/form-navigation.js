﻿function loadResponse(response) {
    if (!response) {
        return;
    };

    var els = $(".form.factory [data-property]");


    $.each(els, function() {
        var el = $(this);
        var property = $(this).attr("data-property");
        var value = "";

        if (property) {
            value = response[property] || "";
        };

        editScrudFormElement(el, value);
    });

    $(".form.factory").hide();
    $(".form.factory").show();
};

$("#FirstButton").click(function () {
    function request() {
        var url = window.scrudFactory.formAPI + "/first";
        return getAjaxRequest(url);
    };

    var ajax = request();

    ajax.success(function(response) {
        loadResponse(response);
    });
});

$("#PreviousButton").click(function () {
    function request(id) {
        var url = window.scrudFactory.formAPI + "/previous/" + id;
        return getAjaxRequest(url);
    };

    var key = $("[data-primarykey]").val();
    if (!key) {
        return;
    };

    var ajax = request(key);

    ajax.success(function (response) {
        loadResponse(response);
    });
});

$("#NextButton").click(function () {
    function request(id) {
        var url = window.scrudFactory.formAPI + "/next/" + id;
        return getAjaxRequest(url);
    };

    var key = $("[data-primarykey]").val();

    if (!key) {
        return;
    };

    var ajax = request(key);

    ajax.success(function (response) {
        loadResponse(response);
    });
});


$("#LastButton").click(function () {
    function request() {
        var url = window.scrudFactory.formAPI + "/last";
        return getAjaxRequest(url);
    };

    var ajax = request();

    ajax.success(function (response) {
        loadResponse(response);
    });
});

