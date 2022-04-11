import Popup from './Popup';
import ButtonLoading from './ButtonLoading';

class AccountPatreon
{
    watch()
    {
        if (mog.path != 'account') {
            return;
        }

        this.handlePatreonStatusCheck();
        this.handlePatreonFriendAssignment();
    }

    /**
     * Handles the button for checking patreon status on the account page.
     */
    handlePatreonStatusCheck()
    {
        const $button = $('.check_patreon_status');

        $button.on('click', event => {
            ButtonLoading.start($button);

            $.ajax({
                url: mog.urls.account.check_patreon,
                success: response => {
                    if (response.ok) {
                        Popup.success('Patreon Confirmed!', 'Your support is much appreciated. Refresh the site to see the changes :) - Thank you');
                        return;
                    }

                    Popup.error('There was a problem (code: 22)', 'Could not detect Patreon status, please make sure you are in the XIVAPI Admin and have accepted your Discord Reward on Patreon, if you have problems, message Miu on Discord.');
                },
                error: (a,b,c) => {
                    Popup.error('There was a problem (code: 47)', 'Please jump on discord and message Miu with the error code to sort this out! Thank you');
                    console.error(a,b,c);
                },
                complete: () => {
                    ButtonLoading.finish($button);
                }
            })
        });
    }

    /**
     * Handles assigning friend the benefit perk
     */
    handlePatreonFriendAssignment()
    {
        $('.pat-friend-promote button').on('click', event => {
            const $btn = $(event.target);
            const id   = $btn.attr('id');

            ButtonLoading.start($btn);

            $.ajax({
                url: '/account/patreon/perks/benefit',
                data: {
                    id: id,
                },
                success: response => {
                    if (response === 10) {
                        Popup.success('All Good!', 'This member now has benefit patreon features!');
                    }

                    if (response === 20) {
                        Popup.success('All Good!', 'This users patreon benefits status has been removed.');
                    }

                    $btn.after('<p>✔</p>');
                    $btn.remove();
                },
                error: (response,b,c) => {
                    const error = response.responseJSON;
                    Popup.error('Error', error.Message);
                },
                complete: () => {
                    ButtonLoading.finish($btn);
                }
            });

            console.log(id);
        })
    }
}

export default new AccountPatreon;
