import Popup from "./Popup";

const Cookie = require("js-cookie");
import Modals from "./Modals";

class Settings
{
    constructor()
    {
        this.uiModal              = $(".modal_settings");
        this.uiModalButton        = $(".btn-settings");

        this.defaultLanguage      = "en";
        this.defaultTimezone      = Intl.DateTimeFormat().resolvedOptions().timeZone;
        this.defaultLeftNav       = "off";
        this.defaultHomeWorld     = "no";

        if (this.defaultTimezone === "undefined") {
            this.defaultTimezone = "Europe/London";
        }

        this.storageKeyServer     = "mogboard_server";
        this.storageKeyLanguage   = "mogboard_language";
        this.storageKeyTimezone   = "mogboard_timezone";
        this.storageKeyLeftNav    = "mogboard_leftnav";
        this.storageKeyHomeWorld  = "mogboard_homeworld";

        this.server               = this.getServer();
        this.language             = this.getLanguage();
        this.timezone             = this.getTimezone();
        this.leftnav              = this.getLeftNav();
        this.homeworld            = this.getHomeWorld();
    }

    init()
    {
        let server    = this.getServer(),
            language  = this.getLanguage(),
            timezone  = this.getTimezone(),
            leftnav   = this.getLeftNav(),
            homeworld = this.getHomeWorld();

        language = language ? language : this.defaultLanguage;
        timezone = timezone ? timezone : this.defaultTimezone;
        leftnav  = leftnav ? leftnav : this.defaultLeftNav;
        homeworld  = homeworld ? homeworld : this.defaultHomeWorld;

        // if not set, ask to set
        if (server === null || server.length === 0) {
            setTimeout(() => {
                this.uiModalButton.trigger("click");
            }, 500);
        } else {
            this.setServer(server);
        }

        this.setLanguage(language);
        this.setTimezone(timezone);
        this.setLeftNav(leftnav);
        this.setHomeWorld(homeworld);

        // set selected items
        this.uiModal.find("select.servers").val(server);
        this.uiModal.find("select.languages").val(language);
        this.uiModal.find("select.timezones").val(timezone);
        this.uiModal.find("select.leftnav").val(leftnav);
        this.uiModal.find("select.homeworld").val(homeworld);
    }

    watch()
    {
        Modals.add(this.uiModal, this.uiModalButton);

        // server select
        this.uiModal.find(".servers").on("change", event => {
            this.setServer($(event.currentTarget).val());
        });

        // language select
        this.uiModal.find(".languages").on("change", event => {
            this.setLanguage($(event.currentTarget).val());
        });

        // timezone select
        this.uiModal.find(".timezones").on("change", event => {
            this.setTimezone($(event.currentTarget).val());
        });

        // left-nav select
        this.uiModal.find(".leftnav").on("change", event => {
            this.setLeftNav($(event.currentTarget).val());
        });

        // home world tab select
        this.uiModal.find(".homeworld").on("change", event => {
            this.setHomeWorld($(event.currentTarget).val());
        });

        // click save
        this.uiModal.find(".btn-green").on("click", event => {
            Popup.success("Settings Saved", "Refreshing site, please wait...");
            location.reload(true);
        })
    }

    getLocalStorageSetting(key, defaultValue)
    {
        const value = localStorage.getItem(key);

        if (value) {
            return value;
        }

        return defaultValue;
    }

    setLocalStorageSetting(key, value)
    {
        localStorage.setItem(key, value);
        Cookie.set(key, value, { expires: 365, path: "/", sameSite: "none", secure: true });
    }

    setServer(server)
    {
        this.setLocalStorageSetting(this.storageKeyServer, server);
    }

    getServer()
    {
        return this.getLocalStorageSetting(this.storageKeyServer, null);
    }

    setLanguage(language)
    {
        this.setLocalStorageSetting(this.storageKeyLanguage, language);
    }

    getLanguage()
    {
        return this.getLocalStorageSetting(this.storageKeyLanguage, this.defaultLanguage);
    }

    getGameDataSource()
    {
        if (this.getLanguage() === "chs") {
            return "https://cafemaker.wakingsands.com";
        }
        return "https://xivapi.com";
    }

    setTimezone(timezone)
    {
        this.setLocalStorageSetting(this.storageKeyTimezone, timezone);
    }

    getTimezone()
    {
        return this.getLocalStorageSetting(this.storageKeyTimezone, this.defaultTimezone);
    }

    setLeftNav(leftnav)
    {
        this.setLocalStorageSetting(this.storageKeyLeftNav, leftnav);
    }

    getLeftNav()
    {
        return this.getLocalStorageSetting(this.storageKeyLeftNav, this.defaultLeftNav);
    }

    setHomeWorld(homeworld)
    {
        this.setLocalStorageSetting(this.storageKeyHomeWorld, homeworld);
    }

    getHomeWorld()
    {
        return this.getLocalStorageSetting(this.storageKeyHomeWorld, this.defaultHomeWorld);
    }
}

export default new Settings;
