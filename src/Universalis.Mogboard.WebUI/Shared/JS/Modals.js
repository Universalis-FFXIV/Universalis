class Modals
{
    constructor()
    {
        this.modalCover = $('.modal_cover');
        this.modelCloseButton = $('.modal_close_button');
    }

    add(modal, button)
    {
        // open the modal
        button.on('click', event => {
            this.modalCover.addClass('open');
            modal.addClass('open');
        });

        // close modal
        this.modalCover.on('click', event => {
            this.modalCover.removeClass('open');
            modal.removeClass('open');
        });

        this.modelCloseButton.on('click', event => {
            this.modalCover.removeClass('open');
            modal.removeClass('open');
        });
    }

    close(modal)
    {
        this.modalCover.removeClass('open');
        modal.removeClass('open');
    }
}

export default new Modals;
