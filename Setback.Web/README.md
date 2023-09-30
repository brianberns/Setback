# Setback web site

## Server

1. Obtain SQLite database `Setback.db` via CFRM and place it in the `Server` directory.

2. Obtain a version of `Fable.Remoting.Suave` that works with recent versions of Suave, such as [this one](https://github.com/brianberns/Fable.Remoting.Suave).

3. Build `Setback.Web.Server`, then build and run `Setback.Web.Harness`.


## Client

In the `Client` directory, run:

    npm install
    npm start

Then browse to http://localhost:8081/.
