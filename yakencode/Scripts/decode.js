function decode() {
    txt = escape($(".textarea_jm").val());
    $(".textarea_result").val("");
    $.post("decode/decode", { srctext: txt, format: $("#format")[0].checked, tagkeep: $("#tagkeep")[0].checked, everyk: $("#everyk")[0].checked, tagsmultiline: $("#tagsmultiline")[0].checked, stringdecode: $("#stringdecode")[0].checked,}, function (result) {
        $(".textarea_result").val(result);
    });
};