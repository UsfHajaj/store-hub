import { Component, input } from '@angular/core';
import { SiTranslatePipe } from '../../pipes/si-translate.pipe';

let brandMarkSeq = 0;

@Component({
  selector: 'app-brand-mark',
  imports: [SiTranslatePipe],
  templateUrl: './brand-mark.component.html',
  styleUrl: './brand-mark.component.scss',
})
export class BrandMarkComponent {
  /** sm = sidebar, md = auth forms, lg = hero */
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly showLabel = input(false);

  /** Unique gradient id so multiple marks on one page don't clash */
  readonly gradId = `sh-grad-${++brandMarkSeq}`;
}
