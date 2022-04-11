import Http from "./Http";
import Settings from './Settings';

class HeaderCategories
{
    constructor()
    {
        this.uiButton       = $('.btn-market-board');
        this.uiView         = $('.market-board-container');
        this.uiCategory     = $('.market-category-container');
        this.uiCategoryView = $('.market-category-view > div');
        this.uiLazy         = null;
        this.viewActive     = false;
        this.categories     = {};
    }

    watch()
    {
        this.uiButton.on('click', event => {
            window.scrollTo(0,0);
            this.uiView.toggleClass('open');
        });

        this.uiView.find('button').on('click', event => {
            window.scrollTo(0,0);

            const id = $(event.currentTarget).attr('id');
            this.openCategory(id);
        });

        $(document).mouseup(event => {
            const btn = this.uiButton;
            const view = this.uiView;

            // if the target of the click isn't the container nor a descendant of the container
            if (!btn.is(event.target) && btn.has(event.target).length === 0
                && !view.is(event.target) && view.has(event.target).length === 0) {
                this.uiView.removeClass('open');
            }
        });

        $(document).mouseup(event => {
            const category = this.uiCategory;

            // if the target of the click isn't the container nor a descendant of the container
            if (!category.is(event.target) && category.has(event.target).length === 0) {
                this.uiCategory.removeClass('open');
                this.viewActive = false;
            }
        });

        $(document).mouseup(event => {
            const category = this.uiCategoryView.parent();

            // if the target of the click isn't the container nor a descendant of the container
            if (!category.is(event.target) && category.has(event.target).length === 0) {
                this.uiCategoryView.parent().removeClass('open');
                this.viewActive = false;
            }
        });

        this.uiCategory.find('.market-category').on('click', 'a', event => {
            this.uiCategory.find('.market-category').html('<div class="loading"><img src="/i/svg/loading2.svg"></div>');
        });

        $('aside .nav-box button').on('click', event => {
            const id = $(event.currentTarget).attr('id');
            const items = this.categories[id];

            this.uiCategoryView.parent().addClass('open');

            // print items
            this.uiCategoryView.html('');
            this.uiCategoryView.append('<div class="gap"></div>');

            items.forEach(item => {
                const id     = item[0];
                const name   = item[1];
                const icon   = 'https://xivapi.com' + item[2];
                const ilv    = item[3];
                const rarity = item[4];
                const url    = mog.url_item.replace('-id-', id);
                const role   = item[5];

                this.uiCategoryView.append(
                    `<a href="${url}" class="rarity-${rarity}">
                        <span>
                            <img src="https://xivapi.com/mb/loading.svg" class="lazy" data-src="${icon}">
                        </span>
                        <span>
                            <div><span class="item-level">${ilv}</span> ${name}</div>
                            <small>${role}</small>
                        </span>
                    </a>`
                );
            });
            this.uiCategoryView.append('<div class="gap"></div>');
            this.uiCategoryView.append('<div class="gap"></div>');

            const uiLazy = $('.lazy').Lazy({
                // your configuration goes here
                scrollDirection: 'vertical',
                appendScroll: $('.item-category-list2'),
                effect: 'fadeIn',
                visibleOnly: false,
                bind: 'event',
            });

            const el = new SimpleBar(document.getElementById('item-category-list2'));
            el.getScrollElement().addEventListener('scroll', event => {
                uiLazy.data("plugin_lazy").update();
            });
        });
    }

    loadCategories()
    {
        Http.getItemCategories(Settings.getLanguage(), response => {
            this.categories = response;
        });
    }

    openCategory(id)
    {
        this.uiView.removeClass('open');
        this.uiCategory.addClass('open');

        this.uiCategory.find('.market-category').html('');

        const category = cats[id];
        const items    = this.categories[id];
        const results  = [];

        items.forEach(item => {
            const id     = item[0];
            const name   = item[1];
            const icon   = 'https://xivapi.com' + item[2];
            const ilv    = item[3];
            const rarity = item[4];
            const url    = mog.url_item.replace('-id-', id);

            results.push(
                `<a href="${url}" class="rarity-${rarity}">
                    <span class="item-icon"><img src="http://xivapi.com/mb/loading.svg" class="lazy" data-src="${icon}"></span>
                    <span class="item-level">${ilv}</span>
                    ${name}
                </a>`
            );
        });

        // render results
        this.uiCategory.find('.market-category').html(`
            <div class="item-category-header">
                <div>
                    ${category.Name} - ${items.length} items
                </div>
                <div>&nbsp;</div>
            </div>
            <div data-simplebar class="item-category-list" id="item-category-list">${results.join('')}</div>
        `);

        this.viewActive = true;

        this.uiLazy = $('.lazy').Lazy({
            // your configuration goes here
            scrollDirection: 'vertical',
            appendScroll: $('.item-category-list'),
            effect: 'fadeIn',
            visibleOnly: false,
            bind: 'event',
        });

        this.setSearchHeight();

        const el = new SimpleBar(document.getElementById('item-category-list'));
        el.getScrollElement().addEventListener('scroll', event => {
            this.uiLazy.data("plugin_lazy").update();
        });
    }

    setSearchHeight()
    {
        if (this.viewActive) {
            // Handle height of search
            const $searchResults = $('.item-category-list');
            const windowHeight = Math.max(document.documentElement.clientHeight, window.innerHeight || 0) - 260;
            $searchResults.css({ height: `${windowHeight}px`} );
        }
    }

    setLoadingLazyLoadWatcher()
    {
        const el = new SimpleBar(document.getElementById('item-category-list'));
        el.getScrollElement().addEventListener('scroll', event => {
            this.uiLazy.data("plugin_lazy").update();
        });
    }
}

export default new HeaderCategories;
