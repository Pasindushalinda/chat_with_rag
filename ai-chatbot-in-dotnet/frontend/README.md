# Let's Build It: AI Chatbot Using Your Data in .NET

## Front End

The frontend is provided for you and consists of 4 HTML files in this folder.  Each one will connect to your .NET API from the [backend](../backend/) folder in this repository.  The frontend expects your API to be running on [http://localhost:5108/](http://localhost:5108/) (note: no https).

### Running The Front End

You can serve the frontend files using [NodeJS](https://nodejs.org/).  With NodeJS installed run this from the command line inside this folder:

```sh
npx serve .
```

Your terminal should show:

```sh
   ┌────────────────────────────────────────┐
   │                                        │
   │   Serving!                             │
   │                                        │
   │   - Local:    http://localhost:3000    │
   │   - Network:  http://10.5.0.2:3000     │
   │                                        │
   │   Copied local address to clipboard!   │
   │                                        │
   └────────────────────────────────────────┘
```

Then open the local URL ([http://localhost:3000](http://localhost:3000)) in your browser.

### Selecting A Route

When you open the local url you will be given links to the 4 .html files in this folder.  They are used to test different iterations of the project.

| Step | URL               | Description |
| ---- | ----------------- | ------------- |
| 1    | [http://localhost:3000/searchlandmarks](http://localhost:3000/searchlandmarks) | Frontend for search service that indexed only the introduction of each Wikipedia article |
| 2    | [http://localhost:3000/searchchunks](http://localhost:3000/searchchunks) | Frontend for search service that indexed the entire article and split it into sections and chunks |
| 3    | [http://localhost:3000/question](http://localhost:3000/question) | Question answering service for RAG |
| 4    | [http://localhost:3000/chat](http://localhost:3000/chat) | The final chatbot front end |
