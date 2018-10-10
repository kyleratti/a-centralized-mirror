export interface Keeper {
    /** Called to seed the database */
    start(): void;
}
