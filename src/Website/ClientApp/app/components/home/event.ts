import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject, PLATFORM } from 'aurelia-framework';
import { Router, RouterConfiguration } from "aurelia-router";
import { DataStore, IHousehold } from "../../DataStore";

@autoinject()
export class EventModel {
    public constructor(
        protected http: HttpClient,
        public dataStore: DataStore
    ){
        
    }

    public event: any;
    public house: IHousehold;

    router: Router;

    configureRouter(config: RouterConfiguration, router: Router) {
        config.map([
            { route: 'start', name: 'start', moduleId: PLATFORM.moduleName("../Register/Start"), title: "First Time" },
            { route: 'family', name: 'family', moduleId: PLATFORM.moduleName("../Register/Family"), title: "Household" },
            { route: 'review', name: 'review', moduleId: PLATFORM.moduleName("../Register/Review"), title: "Review" }
        ]);

        this.router = router;
    }

    async activate(params) {
        let response = await this.http.fetch(`/api/Event/${params.id}`);

        this.event = await response.json();
    }
}
