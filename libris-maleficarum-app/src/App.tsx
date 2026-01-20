import { TopToolbar } from './components/TopToolbar/TopToolbar'
import { WorldSidebar } from './components/WorldSidebar/WorldSidebar'
import { MainPanel } from './components/MainPanel/MainPanel'
import { ChatPanel } from './components/ChatPanel/ChatPanel'
import { DeleteConfirmationModal } from './components/MainPanel/DeleteConfirmationModal'

function App() {
  return (
    <div className="h-screen flex flex-col bg-background text-foreground">
      <TopToolbar />
      <div className="flex-1 flex overflow-hidden">
        <WorldSidebar />
        <MainPanel />
        <ChatPanel />
      </div>
      <DeleteConfirmationModal />
    </div>
  )
}

export default App
