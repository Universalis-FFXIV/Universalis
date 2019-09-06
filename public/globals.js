var lang = "en";
let dataCenterCookie = getCookie("dataCenter");
var dataCenter = dataCenterCookie ? dataCenterCookie : "Aether";
var world = "";
var itemID;

var asyncInitCount = 3; // The number of asynchronous initialization functions that need to finish before post-init

var itemCategories = [null];
var worldList;
const worldMap = new Map();

const months = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
const romanNumerals = [undefined, "I", "II", "III", "IV", "V", "VI", "VII", "VIII"];
