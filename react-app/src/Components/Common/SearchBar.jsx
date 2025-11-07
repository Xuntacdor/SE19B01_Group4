import React, { useState, useEffect } from "react";
import "./SearchBar.css";

const SearchBar = ({ onSearch }) => {
  const [query, setQuery] = useState("");

  // ✅ Debounce: chỉ gọi onSearch sau khi user dừng gõ 500ms
  useEffect(() => {
    const timeout = setTimeout(() => {
      onSearch(query);
    }, 500); // bạn có thể chỉnh 300–800ms tùy độ nhạy

    return () => clearTimeout(timeout);
  }, [query, onSearch]);

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        onSearch(query); // vẫn cho phép nhấn Enter tìm ngay
      }}
    >
      <input
        type="text"
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search..."
      />
      <button type="submit">Search</button>
    </form>
  );
};

export default SearchBar;
