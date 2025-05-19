import { Component } from '@angular/core';

@Component({
  selector: 'app-bit-beam',
  templateUrl: './bit-beam.component.html',
  styleUrls: ['./bit-beam.component.css']
})
export class BitBeamComponent {
  buildings = [
    {
      name: 'Building A',
      documents: ['Document1.pdf', 'Document2.pdf']
    }
  ];
}
