(function() {
    $("button").click(function () {
        $.ajax({
            url: "api/main",
            type: "Post",
            data: JSON.stringify({ url: $("#url").val(), email: $("#email").val() }),
            contentType: 'application/json',
            success: function (data) { alert("Success"); },
            error: function () { alert('Error'); }
        });
    });
})();