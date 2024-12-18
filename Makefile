sqlc-generate:
    sqlc generate -f db/sqlc.yaml

delete-scrape-dir:
    rm -rf /tmp/open-source-literature/scraped

run-search-ui:
    nohup cd search-ui && npm run dev > search-ui.log &

run-data-loader: delete-scrape-dir
    nohup dotnet run --project DataLoader > DataLoader.log &
    
run-local: run-data-loader run-search-ui
    