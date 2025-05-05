import { Component, Input, OnInit, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

interface ChatMessage {
  content: string;
  sender: 'user' | 'assistant';
  timestamp: Date;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.component.html',
  styleUrl: './chatbot.component.scss'
})
export class ChatbotComponent implements OnInit, AfterViewChecked {
  @Input() selectedDocument: File | null = null;
  @ViewChild('chatMessages') private messagesContainer!: ElementRef;
  
  messages: ChatMessage[] = [];
  userInput: string = '';
  isProcessing: boolean = false;
  private shouldScrollToBottom: boolean = false;
  
  ngOnInit(): void {
    // Add initial greeting message
    this.addAssistantMessage('Hello! I\'m your AI assistant. Select a document from the sidebar or upload a new one, then ask me any questions about it.');
  }
  
  sendMessage(): void {
    if (!this.userInput.trim()) return;
    
    // Add user message
    this.addUserMessage(this.userInput);
    const userQuestion = this.userInput;
    this.userInput = '';
    
    // Set processing state
    this.isProcessing = true;
    
    // Simulate AI response after a delay
    setTimeout(() => {
      let response = '';
      
      if (!this.selectedDocument) {
        response = 'Please select a document first so I can answer questions about it.';
      } else {
        // In a real implementation, this would call an API to process the document and question
        response = `I've analyzed ${this.selectedDocument.name}. ${this.generateSampleResponse(userQuestion)}`;
      }
      
      this.addAssistantMessage(response);
      this.isProcessing = false;
    }, 1500);
  }
  
  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  private scrollToBottom(): void {
    try {
      // Make sure the messages container exists and is rendered
      if (this.messagesContainer && this.messagesContainer.nativeElement) {
        this.messagesContainer.nativeElement.scrollTop = this.messagesContainer.nativeElement.scrollHeight;
      }
    } catch (err) {
      console.error('Error scrolling to bottom:', err);
    }
  }
  
  private addUserMessage(content: string): void {
    this.messages.push({
      content,
      sender: 'user',
      timestamp: new Date()
    });
    this.shouldScrollToBottom = true;
  }
  
  private addAssistantMessage(content: string): void {
    this.messages.push({
      content,
      sender: 'assistant',
      timestamp: new Date()
    });
    this.shouldScrollToBottom = true;
  }
  
  private generateSampleResponse(question: string): string {
    // This is a placeholder. In a real implementation, this would be replaced with actual LLM responses
    const responses = [
      'Based on the document, I can tell you that this information relates to the main topic discussed on page 3.',
      'The document mentions several key points related to your question. The most relevant section is on page 7.',
      'According to the document, the answer to your question involves three main factors that are explained in detail.',
      'I found relevant information about your query in the document. Would you like me to summarize the key points?',
      'The document provides comprehensive information on this topic. The main conclusion is that further research is needed.'
    ];
    
    return responses[Math.floor(Math.random() * responses.length)];
  }
}
