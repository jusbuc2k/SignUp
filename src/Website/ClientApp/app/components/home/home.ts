import { HttpClient, json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";

@autoinject()
export class Home {
    public constructor(
        protected http: HttpClient,
        protected router: Router
    ){
    
    }

    public events: any[];

    async activate() {
        let response = await this.http.fetch("/api/Event");

        this.events = await response.json();
    }

    eventClicked(eventID) {
        this.router.navigateToRoute("event", { id: eventID });
    }

}
