import ButtonLoading from "./ButtonLoading";
import Errors from "./Errors";
import Modals from "./Modals";
import Popup from "./Popup";
import Ajax from "./Ajax";

class ProductLists
{
    constructor()
    {
        this.uiModal       = $('.list_modal');
        this.uiModalButton = $('.btn_addto_list');
        this.uiFaveButton  = $('.btn_addto_fave');
        this.uiCreateForm  = $('.create_list_form');
        this.uiListsView   = $('.user_lists');

    }

    watch()
    {
        // add modals
        Modals.add(this.uiModal, this.uiModalButton);

        // load lists
        this.loadLists();

        // on submitting a new
        this.uiCreateForm.on('submit', event => {
            event.preventDefault();

            this.create(
                this.uiCreateForm.find('#list_name').val().trim(),
                itemId
            );
        });

        // on adding to an existing list
        this.uiCreateForm.on('click', '.user_list_existing_button', event => {
            const listId = $(event.currentTarget).attr('data-id');
            const action = $(event.currentTarget).attr('data-action');

            action == 'add' ? this.addItem(listId, itemId) : this.removeItem(listId, itemId);
        });

        // on fave clicking
        this.uiFaveButton.on('click', event => {
            this.addToFavourite();
        });
    }

    /**
     * Render the users list
     */
    loadLists()
    {
        const data = {
            itemId: itemId
        };

        const success = html => {
            this.uiListsView.html(html);
        };

        Ajax.get(mog.urls.lists.render, data, success);
    }

    /**
     * Add an item to your favourites.
     */
    addToFavourite()
    {
        ButtonLoading.start(this.uiFaveButton);

        const data = {
            itemId: itemId,
        };

        const success = response => {
            response.state ? this.uiFaveButton.addClass('on') : this.uiFaveButton.removeClass('on');

            this.uiFaveButton.find('span').text(response.state ? 'Faved' : 'Favourite');
            Modals.close(this.uiModal);
            Popup.success(
                response.state ? 'Added to Favourites' : 'Removed from Favourites',
                response.state ? 'Added to your favourites.' : 'Removed from your favourites.'
            );

            this.loadLists();
        };

        const complete = () =>{
            ButtonLoading.finish(this.uiFaveButton);
        };

        Ajax.post(mog.urls.lists.favourite, data, success, complete, Errors.lists.couldNotFavourite);
    }

    /**
     * Create a new list
     */
    create(name, itemId)
    {
        const $button = this.uiCreateForm.find('button[type="submit"]');
        ButtonLoading.start($button);

        if (name.length < 3) {
            Popup.error('name too short', 'Your list name is a bit too short, please enter in a name to create a list!');
            return;
        }

        const data = {
            name: name,
            itemId: itemId
        };

        const success = response => {
            this.loadLists();
            Modals.close(this.uiModal);
            Popup.success(
                'Created list',
                'Your new list has been created and the item added to it!'
            );
        };

        const complete = () => {
            ButtonLoading.finish($button);
        };

        Ajax.post(mog.urls.lists.create, data, success, complete, Errors.lists.couldNotCreateNewList);
    }

    /**
     * Add an item to an existing list
     */
    addItem(listId, itemId)
    {
        this.uiListsView.html('<div align="center"><img src="/i/svg/loading3.svg" height="32"></div>');

        const data = {
            itemId: itemId
        };

        const success = response => {
            this.loadLists();
            Modals.close(this.uiModal);
            Popup.success(
                'Added to list',
                'Your item has been saved to this list!'
            );
        };

        Ajax.post(mog.urls.lists.addItem.replace('-id-', listId), data, success, null, Errors.lists.couldNotAddItem);
    }

    /**
     * Remove item from a list
     */
    removeItem(listId, itemId)
    {
        this.uiListsView.html('<div align="center"><img src="/i/svg/loading3.svg" height="32"></div>');

        const data = {
            itemId: itemId
        };

        const success = response => {
            this.loadLists();
            Modals.close(this.uiModal);
            Popup.success(
                'Removed from list',
                'Item has been removed from the list.'
            );
        };

        Ajax.post(mog.urls.lists.removeItem.replace('-id-', listId), data, success, null, Errors.lists.couldNotRemoveItem);
    }
}

export default new ProductLists;
