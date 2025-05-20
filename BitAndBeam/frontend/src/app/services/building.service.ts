import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class BuildingService {
  private buildingsSource = new BehaviorSubject<{ name: string; documents: string[] }[]>([]);
  buildings$ = this.buildingsSource.asObservable();

  getBuildings() {
    return this.buildingsSource.getValue();
  }

  updateBuildings(buildings: { name: string; documents: string[] }[]) {
    this.buildingsSource.next(buildings);
  }

  addBuilding(name: string) {
    const current = this.getBuildings();
    this.updateBuildings([...current, { name, documents: [] }]);
  }

  addDocumentToBuilding(index: number, doc: string) {
    const buildings = this.getBuildings();
    buildings[index].documents.push(doc);
    this.updateBuildings([...buildings]);
  }

  deleteBuilding(index: number) {
    const updated = this.getBuildings().filter((_, i) => i !== index);
    this.updateBuildings(updated);
  }
}
