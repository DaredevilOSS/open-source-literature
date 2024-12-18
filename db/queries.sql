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

-- name: InsertMissingTitles :exec
INSERT INTO titles (text_id, source, source_updated_at, author, title, release_date)
SELECT DISTINCT i.text_id, i.source, source_updated_at, i.author, i.title, i.release_date
  FROM interim_texts i
 WHERE NOT EXISTS (
     SELECT 1
       FROM titles w
      WHERE SIMILARITY(w.title, i.title) >= sqlc.arg('certainty')::float8
 );

-- name: InsertMissingPages :exec
INSERT INTO pages (text_id, page, text)
SELECT i.text_id, i.page, i.text
  FROM interim_texts i
 WHERE NOT EXISTS (
    SELECT 1
      FROM pages p
     WHERE p.text_id = i.text_id
       AND p.page = i.page
 )
   AND EXISTS (
    SELECT 1
      FROM titles t
     WHERE t.text_id = i.text_id
 );
    
-- name: SearchInTexts :many
SELECT t.text_id, source, author, title,
       TS_HEADLINE('english', text, TO_TSQUERY(sqlc.arg('query')), sqlc.arg('options'))::text AS matches
  FROM titles t
  JOIN pages p
    ON t.text_id = p.text_id
 WHERE (
    text @@ TO_TSQUERY(sqlc.arg('query')) OR
    text2 @@ TO_TSQUERY(sqlc.arg('query'))
)
 LIMIT sqlc.arg('limit');