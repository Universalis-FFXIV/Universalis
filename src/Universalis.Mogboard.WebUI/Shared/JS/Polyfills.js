if (!Object.prototype.forEach) {
    Object.defineProperty(Object.prototype, 'forEach', {
        value: function (callback, thisArg) {
            if (this === null) {
                throw new TypeError('Not an object');
            }
            thisArg = thisArg || window;
            for (let key in this) {
                if (this.hasOwnProperty(key)) {
                    callback.call(thisArg, this[key], key, this);
                }
            }
        }
    });
}
