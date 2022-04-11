import ClickEvent from "./ClickEvent";

class Header
{
    constructor()
    {
        this.uiButtonMenu = $('.btn-menu');
        this.uiViewMenu   = $('.site-menu')
    }

    watch()
    {
        ClickEvent.watchForMenuClassToggle(this.uiButtonMenu, this.uiViewMenu);
        ClickEvent.watchForMouseOut(this.uiViewMenu);
    }
}

export default new Header;
