import xivapi from "./XIVAPI";
import Popup from "./Popup";
import ButtonLoading from "./ButtonLoading";

class AccountCharacters
{
    constructor()
    {
        this.uiAddCharacterResponse = $(".character_add_response");
    }

    watch()
    {
        if (mog.path != "account") {
            return;
        }

        this.handleNewCharacterSearch();
    }

    /**
     * Handles adding a new character
     */
    handleNewCharacterSearch()
    {
        const $button = $(".character_add");

        // add character clicked
        $button.on("click", event => {
            // grab entered info
            const character = {
                string: $("#character_string").val().trim(),
                server: $("#character_server").val().trim(),
            };

            // validate IDs
            let lodestoneId = null;
            if (character.string.indexOf("finalfantasyxiv.com") > -1) {
                character.string = character.string.split("/");
                character.string = character.string[5];
                lodestoneId = character.string;
            }

            if (character.string.indexOf(" ") == -1) {
                lodestoneId = character.string;
            }

            if (character.string.length == 0) {
                Popup.error("Nothing entered?", "I think you forgot to type something...");
                return;
            }

            ButtonLoading.start($button);

            // if lodestone id, we good to go
            if (lodestoneId) {
                this.handleNewCharacterViaLodestoneId(lodestoneId);
                return;
            }

            // else search and find a lodestone id.
            const name = character.string.split(" ");
            
            this.uiAddCharacterResponse.html("Searching Lodestone for your character...");
            fetch(`/lodestone/search/character/${character.server}/${name[0]}/${name[1]}`)
                .then(response => response.json())
                .then(data => this.handleNewCharacterViaLodestoneId(data.id))
                .catch(err => {
                    Popup.error("Not Found (code 8)", "Could not find your character on Lodestone, try entering the Lodestone URL for your character.");
                    ButtonLoading.finish($button);
                    this.uiAddCharacterResponse.html("");
                });
        });
    }

    /**
     * Handle a character via their lodestone id
     */
    handleNewCharacterViaLodestoneId(lodestoneId, reCalled)
    {
        const $button = $(".character_add");
        this.uiAddCharacterResponse.html("Searching Lodestone for your character...");

        fetch(`/lodestone/character/${lodestoneId}`)
            .then(response => response.json())
            .then(data => {
                this.uiAddCharacterResponse.html("Character found, verifying auth code.");
            
                const verifyCodeIdx = data.bio.search(verify_code);
                console.log(verifyCodeIdx);
                if (verifyCodeIdx === -1) {
                    Popup.error("Auth Code Not Found",  `Could not find your auth code (${verify_code}) on your characters profile, try again!`);
                    this.uiAddCharacterResponse.html("");
                    ButtonLoading.finish($button);
                    return;
                }
            
                this.uiAddCharacterResponse.html("Auth code found, adding character...");
            
                $.ajax({
                    url: mog.urls.characters.add.replace("-id-", lodestoneId),
                    success: response => {
                        if (response === true) {
                            Popup.success("Character Added!", "Your character has been added, the page will refresh in 3 seconds.");
                            Popup.setForcedOpen(true);
                            setTimeout(() => {
                                location.reload();
                            }, 3000);

                            return;
                        }

                        Popup.error("Character failed to add", `Error: ${response.Message}`);
                        /*document.getElementById(character_string).reset(); */
                    },
                    error: (a, b, c) => {
                        Popup.error("Something Broke (code 145)", "Could not add your character, please hop on Discord and complain to Kara");
                        console.error(a, b, c);
                    },
                    complete: () => {
                        this.uiAddCharacterResponse.html("");
                        ButtonLoading.finish($button);
                    }
                });
            })
            .catch(err => {
                Popup.error("Character failed to add", `Error: ${err}`);
            });
    }
}

export default new AccountCharacters;
