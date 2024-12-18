import {createClient} from "@connectrpc/connect";
import {createGrpcWebTransport} from "@connectrpc/connect-web";
import {SearchRequest, SearchResponse} from "@/gen/searcher_pb";
import {Searcher} from "@/gen/searcher_connect";

const transport = createGrpcWebTransport({
    baseUrl: "http://localhost:5209",
});

const client = createClient(Searcher, transport);

export default async function runSearch(request: SearchRequest): Promise<SearchResponse> {
    return client.search(request);
}
