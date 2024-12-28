'use client';

import { useState, useEffect, useMemo } from 'react';
import runSearch from "../grpc/client";
import {SearchRequest, SearchResponse, SearchResult} from "@/gen/searcher_pb";

const Search: React.FC = () => {
    const [searchTerm, setSearchTerm] = useState('');
    const [filteredData, setFilteredData] = useState<SearchResult[]>([]);

    useEffect(() => {
        // If searchTerm is empty, you might want to clear results or do nothing
        if (!searchTerm) {
            setFilteredData([]);
            return;
        }

        // Async function inside useEffect
        const performSearch = async () => {
            try {
                const request = SearchRequest.fromJson(
                    {
                        query: searchTerm
                    }
                );
                const response = await runSearch(request);
                setFilteredData(response.results);
            } catch (error) {
                console.error('Error performing search:', error);
                setFilteredData([]);
            }
        };
        performSearch();
    }, [searchTerm]);

    return (
        <div className="max-w-4xl mx-auto px-4 py-6">
            <h1 className="text-2xl font-bold mb-4 text-center">Search &amp; Results</h1>

            {/* Search Bar */}
            <div className="flex items-center space-x-2 mb-6">
                <input
                    type="text"
                    placeholder="Search..."
                    className="border border-gray-300 px-4 py-2 rounded w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                />
            </div>

            {/* Results Table */}
            <div className="overflow-x-auto">
                <table className="min-w-full table-auto border-collapse">
                    <thead>
                    <tr className="bg-gray-200">
                        <th className="py-2 px-4 border-b text-left">Author</th>
                        <th className="py-2 px-4 border-b text-left">Title</th>
                        <th className="py-2 px-4 border-b text-left">Source</th>
                        <th className="py-2 px-4 border-b text-left">Matches</th>
                    </tr>
                    </thead>
                    <tbody>
                    {filteredData.map((item) => (
                        <tr key={item.id} className="hover:bg-gray-100">
                            <td className="py-2 px-4 border-b">{item.author}</td>
                            <td className="py-2 px-4 border-b">{item.title}</td>
                            <td className="py-2 px-4 border-b">{item.source}</td>
                            <td className="py-2 px-4 border-b">{item.matches}</td>
                        </tr>
                    ))}
                    {filteredData.length === 0 && (
                        <tr>
                            <td colSpan={3} className="text-center py-4">
                                No results found.
                            </td>
                        </tr>
                    )}
                    </tbody>
                </table>
            </div>

            {/* Add pagination or other UI below as needed */}
        </div>
    );
};

export default Search;
