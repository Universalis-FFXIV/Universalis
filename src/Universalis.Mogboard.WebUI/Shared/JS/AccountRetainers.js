import xivapi from "./XIVAPI";
import Popup from "./Popup";
import ButtonLoading from "./ButtonLoading";

class AccountRetainers
{
    constructor()
    {
        this.stateAdding = false;

        this.uiAddRetainerResponse = $(".retainer_add_response");
        this.uiItemSearchResponse = $(".retainer_item_search_response");
    }

    watch()
    {
        if (mog.path != "account") {
            return;
        }

        this.handleNewRetainerAdd();
        this.watchItemSearchInput();
        this.watchRetainerConfirmation();
    }

    watchRetainerConfirmation()
    {
        const $button = $(".retainer_confirm");

        $button.on("click", event => {
            ButtonLoading.start($button);
            const id = $(event.currentTarget).attr("data-id");

            $.ajax({
                url: mog.urls.retainers.confirm.replace("-id-", id),
                success: response => {
                    let status = response[0];
                    let message = response[1];

                    if (message.trim().length < 1) {
                        message = "Could not add your retainer, the Companion Servers may be having issues. Please try again soon or contact the discord";
                    }

                    if (status == false) {
                        Popup.error("Not yet!", message);
                        return;
                    }

                    Popup.success("Retainer Confirmed!", "You are all good to go, the retainer is yours! <br> The site will refresh in 3 seconds.");
                    Popup.setForcedOpen(true);
                    setTimeout(() => {
                        location.reload();
                    }, 3000);
                },
                error: (a,b,c) => {
                    console.error(a,b,c);
                },
                complete: () => {
                    ButtonLoading.finish($button);
                    ButtonLoading.disable($button);
                }
            })
        });
    }

    /**
     * Watch item search input
     */
    watchItemSearchInput()
    {
        const $input = $(".retainer_item_search");
        let timeout = null;
        let searched = null;

        $input.on("keyup", event => {
            const string = $input.val().trim();
            clearTimeout(timeout);

            if (string.length < 2 || string == searched) {
                return;
            }

            this.uiItemSearchResponse.html('<div align="center"><img src="/i/svg/loading3.svg" height="32"></div>');

            timeout = setTimeout(() => {
                xivapi.searchLimited(string, response => {
                    searched = string;
                    if (response == null || response.Pagination.Results == 0) {
                        this.uiItemSearchResponse.html("<p>Could not find an item</p>");
                        return;
                    }

                    this.uiItemSearchResponse.html("");
                    response.Results.forEach(item => {
                        this.uiItemSearchResponse.append(
                            `<button class="item_button" data-id="${item.ID}">${item.Name}</button>`
                        );
                    });
                })
            }, 250);
        });

        this.uiItemSearchResponse.on("click", "button", event => {
            const itemId = $(event.currentTarget).attr("data-id");
            const name   = $(event.currentTarget).text();

            $(".retainer_item_search").val(name);
            $(".retainer_add").prop("disabled", false);
            $("#retainer_item").val(itemId);

            this.uiItemSearchResponse.html("");
        })
    }

    /**
     * Handles adding a new retainer
     */
    handleNewRetainerAdd()
    {
        const $button = $(".retainer_add");

        // add retainer clicked
        $button.on("click", event => {
            if (this.stateAdding) {
                return;
            }

            // grab entered info
            const retainer = {
                name: $("#retainer_string").val().trim(),
                server: $("#retainer_server").val().trim(),
                itemId: $("#retainer_item").val().trim(),
            };

            if (retainer.name.length < 2) {
                Popup.error("No name?", "is your retainer name really below 2 characters?");
                return;
            }

            ButtonLoading.start($button);
            this.stateAdding = true;

            $.ajax({
                url: mog.urls.retainers.add,
                data: retainer,
                success: response => {
                    if (response === true) {
                        Popup.success("Retainer Added!", "Your retainer has been added, the page will refresh in 3 seconds.");
                        Popup.setForcedOpen(true);
                        setTimeout(() => {
                            location.reload();
                        }, 3000);

                        return;
                    }

                    Popup.error("Retainer failed to add", `Error: ${response.Message}`);
                },
                error: (a,b,c) => {
                    Popup.error("Something Broke (code 148)", "Could not add your retainer, please hop on discord!");
                    console.error(a,b,c);
                },
                complete: () => {
                    this.stateAdding = false;
                    this.uiAddRetainerResponse.html("");
                    ButtonLoading.finish($button);
                }
            })
        });
    }
}

export default new AccountRetainers;
