import CafeMaker from "./CafeMaker";
import ClickEvent from "./ClickEvent";
import Settings from "./Settings";
import XIVAPI from "./XIVAPI";

/**
 * todo - this needs some cleaning up
 */
class Search
{
    constructor()
    {
        this.uiInput    = $("input.search");
        this.uiView     = $(".search-results-container");
        this.loading    = $(".search-loading");
        this.uiLazy     = null;
        this.searchTerm = null;
    }

    watch()
    {
        this.uiInput.on("keyup", event => {
            this.uiView.removeClass("open");
            this.uiInput.removeClass("complete");

            const chinese = Settings.getLanguage() === "chs";

            /**
             * Check search term
             */
            const searchTerm = $(event.currentTarget).val().trim();
            searchTerm.length === 0
                ? this.uiInput.removeClass("typing")
                : this.uiInput.addClass("typing");

            if (searchTerm.length === 0) {
                this.searchTerm = "";
                this.uiView.removeClass("open");
                return;
            }

            /**
             * Assign stuff
             */

            this.searchTerm = $(event.currentTarget).val().trim();
            this.uiView.addClass("open");

            /**
             * Check for enter button
             */
            if (event.keyCode !== 13) {
                return;
            }

            /**
             * Search
             */
            this.loading.addClass("on");
            (chinese ? CafeMaker : XIVAPI).search(searchTerm, response => {
                this.loading.removeClass("on");
                this.render(response);
            });
        });

        ClickEvent.watchForMouseOut(this.uiView, () => {
            this.uiInput.removeClass("complete typing");
        });

        $(window).on("resize", event => {
            this.setSearchHeight();
        })
    }

    render(response)
    {
        window.scrollTo(0,0);
        this.uiInput.removeClass("typing");
        this.uiInput.addClass("complete");

        const results = [];

        // prep results
        response.Results.forEach((item) => {
            const url = mog.url_item.replace("-id-", item.ID);

            results.push(
                `<a href="${url}" class="rarity-${item.Rarity}">
                    <span class="item-icon"><img src="http://xivapi.com/mb/loading.svg" class="lazy" data-src="${Settings.getGameDataSource()}${item.Icon}"></span>
                    <span class="item-level">${item.LevelItem}</span>
                    ${item.Name}
                    <span class="item-category">${item.ItemSearchCategory.Name}</span>
                </a>`
            );
        });

        // <button class="btn-filters"><icon class="xiv-MarketFilter"></icon> Filters</button>


        // render results
        this.uiView.find(".search-results").html(`
            <div class="item-search-header">
                <div>
                    Found ${response.Pagination.Results} / ${response.Pagination.ResultsTotal} for <strong>${this.searchTerm}</strong>
                </div>
                <div>
                </div>
            </div>
            <div data-simplebar class="item-search-list" id="item-search-list">${results.join("")}</div>
        `);

        this.uiLazy = $(".lazy").Lazy({
            // your configuration goes here
            scrollDirection: "vertical",
            appendScroll: $(".item-search-list"),
            effect: "fadeIn",
            visibleOnly: false,
            bind: "event",
        });

        this.setSearchHeight();

        const el = new SimpleBar(document.getElementById("item-search-list"));
        el.getScrollElement().addEventListener("scroll", event => {
            this.uiLazy.data("plugin_lazy").update();
        });

        this.uiView.addClass("open");
    }

    setSearchHeight()
    {
        if (this.searchTerm) {
            // Handle height of search
            const $searchResults = $(".item-search-list");
            const windowHeight = Math.max(document.documentElement.clientHeight, window.innerHeight || 0) - 260;
            $searchResults.css({ height: `${windowHeight}px`} );
        }
    }
}

export default new Search;
