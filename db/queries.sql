-- name: CopyToInterim :copyfrom
INSERT INTO interim_texts 
    (text_id, source, source_updated_at, author, title, release_date, page, text) 
VALUES (sqlc.arg('text_id'), sqlc.arg('source'), sqlc.arg('source_updated_at'),
        sqlc.arg('author'),sqlc.arg('title'), sqlc.arg('release_date'), 
        sqlc.arg('page'), sqlc.arg('text'));

-- name: TruncateInterim :exec
TRUNCATE TABLE interim_texts;

-- name: GetMaxId :one
SELECT COALESCE((SELECT MAX(text_id) FROM titles), 1)::INTEGER AS max_id;

-- name: InsertTitles :exec
INSERT INTO titles (text_id, source, source_updated_at, author, title, release_date)
SELECT DISTINCT text_id, source, source_updated_at, author, title, release_date
  FROM interim_texts;

-- name: InsertPages :exec
INSERT INTO pages (text_id, page, text)
SELECT text_id, page, text
  FROM interim_texts;
    
-- name: SearchInTexts :many
SELECT t.text_id, source, author, title,
       TS_HEADLINE('english', text, TO_TSQUERY(sqlc.arg('query')), sqlc.arg('options'))::text AS matches
  FROM titles t
  JOIN (SELECT text_id, page, text
          FROM pages
         WHERE trigrams @@ TO_TSQUERY(sqlc.arg('query'))) p
    ON t.text_id = p.text_id
 ORDER BY title, author, source
 LIMIT sqlc.arg('limit');