// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

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
            '/IStudentApi/**': {
                target: "http://127.0.0.1:5000",// assuming the backend server is hosted on port 5000 during development
                changeOrigin: true
            }
        }
    },
    module: {
    }
}
