import Settings from './Settings';
import Home from './Home';
import Header from './Header';
import HeaderUser from './HeaderUser';
import HeaderCategories from './HeaderCategories';
import HighchartsFormatter from './HighchartsFormatter';
import Search from './Search';
import Product from './Product';
import ProductAlerts from './ProductAlerts';
import ProductLists from './ProductLists';
import AccountCharacters from './AccountCharacters';
import AccountRetainers from './AccountRetainers';
import AccountPatreon from './AccountPatreon';
import UniversalisApi from './UniversalisApi';

/**
 * Basic stuff
 */
Settings.init();
Settings.watch();
HeaderUser.watch();
Header.watch();
HeaderCategories.watch();
HeaderCategories.loadCategories();
Search.watch();
Home.watch();

/**
 * Item Pages
 */
if (typeof appEnableItemPage !== 'undefined' && appEnableItemPage === 1) {
    Product.watch();
    ProductAlerts.watch();
    ProductLists.watch();
}

/**
 * Account page
 */
AccountPatreon.watch();

if (typeof appEnableCharacters !== 'undefined' && appEnableCharacters === 1) {
    AccountCharacters.watch();
}

if (typeof appEnableRetainers !== 'undefined' && appEnableRetainers === 1) {
    AccountRetainers.watch();
}

/**
 * Export
 */
export default {
    HeaderCategories,
    HighchartsFormatter,
    UniversalisApi,
}
