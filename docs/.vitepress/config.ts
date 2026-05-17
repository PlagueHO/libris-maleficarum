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
      { text: 'Quickstart Local', link: '/quickstart-local' },
      { text: 'Quickstart Azure', link: '/quickstart-azure' },
      { text: 'Architecture', link: '/design/readme' },
    ],

    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quickstart Local', link: '/quickstart-local' },
          { text: 'Quickstart Azure', link: '/quickstart-azure' },
        ],
      },
      {
        text: 'Design',
        items: [
          { text: 'Design Overview', link: '/design/readme' },
          { text: 'Project Overview', link: '/design/overview' },
          { text: 'Architecture', link: '/design/backend' },
          { text: 'Frontend', link: '/design/frontend' },
          { text: 'Infrastructure', link: '/design/infrastructure' },
          { text: 'Data Model', link: '/design/data_model' },
          { text: 'API', link: '/design/api' },
          { text: 'CI/CD', link: '/design/ci_cd' },
          { text: 'Testing', link: '/design/testing' },
          { text: 'Technology', link: '/design/technology' },
          { text: 'Folder Structure', link: '/design/folder_structure' },
          { text: 'Schema Matrix', link: '/design/schema_version_matrix' },
        ],
      },
      {
        text: 'Other Docs',
        items: [
          { text: 'App Components', link: '/components/libris-maleficarum-app-documentation' },
          { text: 'Markdown Linting', link: '/operations/markdown_linting' },
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
