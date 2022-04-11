class Home
{
    constructor()
    {
    }

    watch()
    {
        $('.home-nav button').on('click', event => {
            const tab = $(event.currentTarget).attr('data-tab');

            $('.home-tab.open').removeClass('open');
            $('#' + tab).addClass('open');
        })
    }
}

export default new Home;
