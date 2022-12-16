CREATE TABLE Todo (
  ID              INT           NOT NULL    IDENTITY    PRIMARY KEY,
  Title           VARCHAR(280)  NOT NULL,
  IsCompleted     BIT,
);
