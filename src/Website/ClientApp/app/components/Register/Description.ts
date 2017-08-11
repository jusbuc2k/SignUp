import { HttpClient , json } from 'aurelia-fetch-client';
import { autoinject } from 'aurelia-framework';
import { Router } from "aurelia-router";
import { EventModel } from "../home/event";

@autoinject()
export class StartModel {
    constructor(
        protected eventModel: EventModel,
        protected router: Router
    ) {
    }

    description: string;
    name: string;
    fees: any[];

    activate(params) {
        this.description = this.eventModel.event.description;
        this.name = this.eventModel.event.name;
        this.fees = this.eventModel.event.fees;
    }

}

