import { Component, input } from '@angular/core';
import { SiTranslatePipe } from '../../pipes/si-translate.pipe';

@Component({
  selector: 'app-auth-visual',
  imports: [SiTranslatePipe],
  templateUrl: './auth-visual.component.html',
  styleUrl: './auth-visual.component.scss',
})
export class AuthVisualComponent {
  /** Which caption set to show under the scene */
  readonly tone = input<'login' | 'register' | 'changePassword'>('login');
}
