import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Libris Maleficarum',
  description: 'AI-enhanced campaign management and world-building documentation.',
  base: '/libris-maleficarum/',
  outDir: 'dist',
  ignoreDeadLinks: true,
  appearance: 'auto',

  themeConfig: {
    nav: [
      { text: 'Quickstart Local', link: '/QUICKSTART-LOCAL' },
      { text: 'Quickstart Azure', link: '/QUICKSTART-AZURE' },
      { text: 'Architecture', link: '/design/README' },
    ],

    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quickstart Local', link: '/QUICKSTART-LOCAL' },
          { text: 'Quickstart Azure', link: '/QUICKSTART-AZURE' },
        ],
      },
      {
        text: 'Design',
        items: [
          { text: 'Design Overview', link: '/design/README' },
          { text: 'Project Overview', link: '/design/OVERVIEW' },
          { text: 'Architecture', link: '/design/BACKEND' },
          { text: 'Frontend', link: '/design/FRONTEND' },
          { text: 'Infrastructure', link: '/design/INFRASTRUCTURE' },
          { text: 'Data Model', link: '/design/DATA_MODEL' },
          { text: 'API', link: '/design/API' },
          { text: 'CI/CD', link: '/design/CI_CD' },
          { text: 'Testing', link: '/design/TESTING' },
          { text: 'Technology', link: '/design/TECHNOLOGY' },
          { text: 'Folder Structure', link: '/design/FOLDER_STRUCTURE' },
          { text: 'Schema Matrix', link: '/design/SCHEMA_VERSION_MATRIX' },
        ],
      },
      {
        text: 'Other Docs',
        items: [
          { text: 'App Components', link: '/components/libris-maleficarum-app-documentation' },
          { text: 'Markdown Linting', link: '/operations/MARKDOWN_LINTING' },
        ],
      },
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/libris-maleficarum' },
    ],

    footer: {
      message: 'Released under the MIT License.',
    },
  },
})
