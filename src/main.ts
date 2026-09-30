import './skins/base.css';
import './skins/skins.css';
import { mount } from 'svelte';
import Root from './Root.svelte';

mount(Root, { target: document.getElementById('app')! });
