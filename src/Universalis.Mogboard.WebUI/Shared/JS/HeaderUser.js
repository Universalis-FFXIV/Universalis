class HeaderUser
{
    constructor()
    {
        this.uiButton = $('.user-btn');
        this.uiMenu = $('.user-menu');
    }

    watch()
    {

        this.uiButton.on('click', event => {
            this.uiMenu.toggleClass('open');
        });

        $(document).mouseup(event => {
            const buttons = this.uiButton;
            const nav = this.uiMenu;

            // if the target of the click isn't the container nor a descendant of the container
            if (!buttons.is(event.target) && buttons.has(event.target).length === 0
                && !nav.is(event.target) && nav.has(event.target).length === 0) {

                this.uiMenu.removeClass('open');
            }
        });
    }
}

export default new HeaderUser;
