-- P-01: viena duomenų bazė, atskira schema kiekvienam servisui.
-- Kiekvienas servisas rašo tik į savo schemą (EF Core migracijos paleidžiamos servisui startuojant).
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS workspace;
CREATE SCHEMA IF NOT EXISTS messaging;
CREATE SCHEMA IF NOT EXISTS search;
CREATE SCHEMA IF NOT EXISTS notification;
