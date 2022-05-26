var path = require("path");

module.exports = {
    mode: "development",
    entry: "./src/App.fs.js",
    output: {
        path: path.join(__dirname, "./public"),
        filename: "bundle.js",
    },
    devServer: {
        static: "./public",
        port: 8081,
        proxy: {
            '/Setback/ISetbackApi/**': {
                target: "http://127.0.0.1:5000/",
                changeOrigin: true
            }
        }
    },
    resolve: {
        alias: {
            jquery: "jquery/src/jquery"   // important - can't import jQuery from dist folder
        }
    },
    module: {
        rules: [
            {
                test: /\.css$/i,
                use: ['style-loader', 'css-loader'],
            },
            {
                test: /\.(png|svg|jpg|jpeg|gif)$/i,
                type: 'asset/resource',
            }
        ]
    }
}
