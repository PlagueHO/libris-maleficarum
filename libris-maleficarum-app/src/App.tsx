import { TopToolbar } from './components/TopToolbar/TopToolbar'
import { WorldSidebar } from './components/WorldSidebar/WorldSidebar'
import { MainPanel } from './components/MainPanel/MainPanel'
import { ChatWindow } from './components/ChatWindow/ChatWindow'

function App() {
  // Temporary: Test if React is rendering
  console.log('App component rendering...');
  
  return (
    <div className="h-screen flex flex-col bg-background text-foreground">
      <TopToolbar />
      <div className="flex-1 flex overflow-hidden">
        <WorldSidebar />
        <MainPanel />
        <ChatWindow />
      </div>
    </div>
  )
}

export default App
