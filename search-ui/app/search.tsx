'use client';

import { useState, FormEvent } from 'react';
import runSearch from "../grpc/client";
import {SearchRequest, SearchResult} from "@/gen/searcher_pb";

export default function Search() {
    const [query, setQuery] = useState('');
    const [results, setResults] = useState<string[]>([]);
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: FormEvent) => {
        e.preventDefault();
        setLoading(true);
        try {
            const request = SearchRequest.fromJson(
                {
                    query: query,
                    // author: "edgar ellen poe"
                }
            );
            const response = await runSearch(request);
            setResults(response.results.map((r: SearchResult) => r.title));
        } catch (error) {
            console.error('Error performing search:', error);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="p-8 max-w-md mx-auto">
            <h1 className="text-2xl font-bold mb-4">Search</h1>
            <form onSubmit={handleSubmit} className="flex gap-2 mb-4">
                <input
                    type="text"
                    placeholder="Enter your query..."
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded focus:outline-none focus:border-blue-500"
                />
                <button
                    type="submit"
                    disabled={loading}
                    className="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-700 disabled:bg-gray-400"
                >
                    {loading ? 'Searching...' : 'Search'}
                </button>
            </form>
            <div>
                <h2 className="text-xl font-semibold mb-2">Results:</h2>
                {loading ? (
                    <p className="text-gray-600">Loading...</p>
                ) : results.length > 0 ? (
                    <ul className="list-disc list-inside space-y-1">
                        {results.map((res, i) => (
                            <li key={i}>{res}</li>
                        ))}
                    </ul>
                ) : (
                    <p className="text-gray-600">No results found.</p>
                )}
            </div>
        </div>
    );
}
