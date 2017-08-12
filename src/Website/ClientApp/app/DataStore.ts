
export class DataStore {

    public getHouse(id: string) : IHousehold {
        let key = `Household_${id}`;
        let houseData = sessionStorage.getItem(key);

        if (houseData) {
            return JSON.parse(houseData);
        } else {
            return undefined;
        }
    }

    public setHouse(id: string, properties: any): void {
        let key = `Household_${id}`;
        let houseData = sessionStorage.getItem(key);
        let house = {};

        if (houseData) {
            house = JSON.parse(houseData);
        }

        Object.assign(houseData, properties);

        sessionStorage.setItem(key, JSON.stringify(houseData));
    }

    public clearHouse(id: string): void {
        let key = `Household_${id}`;
        sessionStorage.removeItem(key);
    }
}

export interface IHousehold {
    id?: string;
    people: any[];
    new?: boolean;
}