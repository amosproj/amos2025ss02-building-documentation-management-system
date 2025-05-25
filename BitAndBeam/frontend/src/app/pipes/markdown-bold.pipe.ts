import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'markdownBold',
  standalone: true,
})
export class MarkdownBoldPipe implements PipeTransform {
  transform(value: string): string {
    if (!value) return '';
    return value.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  }
}
