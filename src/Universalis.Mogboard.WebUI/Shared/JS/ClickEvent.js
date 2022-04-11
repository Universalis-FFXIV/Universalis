class ClickEvent
{
    /**
     * Watches a menu for a click event
     */
    watchForMenuClassToggle(button, menu)
    {
        button.on('click', event => {
            window.scrollTo(0,0);
            menu.toggleClass('open');
        });
    }

    /**
     * Watch for clicking out of an event
     */
    watchForMouseOut(view, callback)
    {
        $(document).mouseup(event => {
            // if the target of the click isn't the container nor a descendant of the container
            if (!view.is(event.target) && view.has(event.target).length === 0) {
                view.removeClass('open');
                return typeof callback == 'undefined' ? null : callback();
            }
        });
    }
}

export default new ClickEvent;
