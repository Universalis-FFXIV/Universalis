import HeaderCategories from "./HeaderCategories";
import Popup from "./Popup";
import ButtonLoading from "./ButtonLoading";

class Product
{
    constructor()
    {
        this.uiButtons  = $('.product .item_nav');
        this.uiTabs     = $('.product .tab');
        this.uiCategory = $('.product .product-search-cat');
        this.uiUpdate   = $('.btn_update');
    }

    watch()
    {
        this.uiButtons.find('button').on('click', event => {
            const tab = $(event.currentTarget).attr('data-tab');
            this.switchTab(event, tab);
        });


        this.uiCategory.on('click', event => {
            const id = $(event.currentTarget).attr('data-cat');
            HeaderCategories.openCategory(id);
        });

        $(document).scroll(event => {
            let y = $(document).scrollTop(),
                menu = this.uiButtons;

            if (y >= 300) {
                menu.addClass('fixed');
            } else {
                menu.removeClass('fixed');
            }
        });

        $('.item_nav_mobile_toggle').on('click', event => {
            $('.item_nav').toggleClass('open');
        });

        // manual update
        $('.btn_update').on('click', event => {
            ButtonLoading.start(this.uiUpdate);

            if (typeof itemId == 'undefined') {
                Popup.error('Error 6D', 'Some how you do not have the item id assigned');
                return;
            }

            $.ajax({
                url: mog.urls.items.update.replace('-id-', itemId),
                success: response => {
                    Popup.success('Update Status', response.message);
                },
                error: (a,b,c) => {
                    Popup.error('Error 8F', 'Could not update item! Try again later.');
                    console.log(a,b,c);
                },
                complete: () => {
                    ButtonLoading.finish(this.uiUpdate);
                }
            })
        });
    }

    /**
     * Change product tab page
     */
    switchTab(event, tab)
    {
        // remove current active states
        this.uiButtons.find('button.open').removeClass('open');
        this.uiTabs.find('.tab-page.open').removeClass('open');

        // set active
        $(event.currentTarget).addClass('open');
        this.uiTabs.find(`.tab-${tab}`).addClass('open');
    }
}

export default new Product;
