CREATE TABLE interim_texts (
    text_id             INTEGER       NOT NULL,
    source              VARCHAR(200)  NOT NULL,
    source_updated_at   DATE,
    author              VARCHAR(200)  NOT NULL,
    title               TEXT          NOT NULL,
    release_date        DATE          NOT NULL,
    page                INTEGER       NOT NULL,
    text                TEXT          NOT NULL
);

CREATE TABLE titles (
    text_id             INTEGER       PRIMARY KEY,
    source              VARCHAR(200)  NOT NULL,
    source_updated_at   DATE,
    author              VARCHAR(200)  NOT NULL,
    title               TEXT          NOT NULL,
    release_date        DATE          NOT NULL
);

CREATE TABLE pages (
   text_id  INTEGER  NOT NULL,
   page     INTEGER  NOT NULL,
   text     TEXT     NOT NULL,
   trigrams TSVECTOR GENERATED ALWAYS AS (to_tsvector('english', text)) STORED,
   PRIMARY KEY (text_id, page),
   FOREIGN KEY (text_id) REFERENCES titles (text_id)
);

CREATE INDEX pages_text_idx ON pages USING GIN (trigrams);
