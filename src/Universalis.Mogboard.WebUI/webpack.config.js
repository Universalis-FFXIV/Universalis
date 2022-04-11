const path = require('path');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = [
    {
        name: 'app',
        entry: './Shared/JS/App.js',
        output: {
            filename: 'app.js',
            path: path.resolve(__dirname, 'wwwroot', 'ui'),
        },
        module: {
            rules: [
                {
                    test: /\.m?js$/,
                    exclude: /(node_modules|bower_components)/,
                    use: {
                        loader: 'babel-loader',
                        options: {
                            presets: ['@babel/preset-env'],
                        },
                    },
                },
            ],
        },
    },
    {
        name: 'ui',
        plugins: [new MiniCssExtractPlugin({
            filename: 'ui.css',
        })],
        entry: './Shared/SCSS/app.scss',
        output: {
            filename: 'ui.css.js',
            path: path.resolve(__dirname, 'wwwroot', 'ui'),
        },
        module: {
            rules: [
                {
                    test: /\.s[ac]ss$/i,
                    use: [
                        MiniCssExtractPlugin.loader,
                        'css-loader',
                        'sass-loader',
                    ],
                },
            ],
        },
        resolve: {
            alias: {
                '/i': path.resolve(__dirname, 'wwwroot', 'i'),
            },
        },
    },
];