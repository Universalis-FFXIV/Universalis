class ButtonLoading
{
    start($ele)
    {
        $ele.attr("data-text", $ele.html());

        $ele.addClass("loading_interaction");
        $ele.prop("disabled", true);
        $ele.css({
            'min-width': `${$ele.outerWidth()}px`,
            'min-height': `${$ele.outerHeight()}px`,
            display: "inline-block",
        });

        $ele.html("&nbsp;");
    }

    finish($ele)
    {
        const text = $ele.attr("data-text");

        $ele.removeClass("loading_interaction");
        $ele.prop("disabled", false);
        $ele.html(text);
    }

    disable($ele, text)
    {
        $ele.attr("data-text",$ele.html());

        $ele.removeClass("loading_interaction");
        $ele.prop("disabled", true);
        $ele.html(text);
    }
}

export default new ButtonLoading;
