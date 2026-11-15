import React, {useEffect, useRef, useState} from "react";
import {Avatar, Button, Drawer, Dropdown, Form, Input, Layout, message, Space, Spin, Tooltip, Typography} from "antd";
import {v4 as uuidv4} from "uuid";
import {
    ArrowUpOutlined,
    AudioOutlined,
    BankOutlined,
    BorderOutlined,
    CheckOutlined,
    ColumnWidthOutlined,
    CopyOutlined,
    CreditCardOutlined,
    DislikeFilled,
    DislikeOutlined,
    DollarOutlined,
    FormOutlined,
    GoldOutlined,
    LikeFilled,
    LikeOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    PercentageOutlined,
    PlusOutlined,
    QuestionCircleOutlined,
    SettingOutlined,
    SlidersOutlined,
    StarOutlined,
    TranslationOutlined,
    UndoOutlined,
} from "@ant-design/icons";
import dayjs from "dayjs";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import rehypeRaw from "rehype-raw";
import "../assets/styles/ChatUI.scss";
import sidebarLogo from "../assets/img/logo.png";
import byblosAvatar from "../assets/img/byblos-avatar.png";
import collapsedLogo from "../assets/img/collapsed-logo.png";
import armFlag from '../assets/img/Flag_of_Armenia.svg.png'
import flagRus from '../assets/img/Flag_of_Russia.svg'
import flagEng from '../assets/img/Flag-United-Kingdom.webp'
import {ConversationsHistory} from "../components/ConversationsHistory";
import axios from "axios";
import {handleRequestErrors} from "../utils/handleRequestErrors";
import utc from "dayjs/plugin/utc";
import timezone from "dayjs/plugin/timezone";

dayjs.extend(utc);
dayjs.extend(timezone);

const { Sider, Content } = Layout;
const { Text } = Typography;

export const ChatMessage = React.memo(function ChatMessage({
                                                               role,
                                                               content,
                                                               displayedContent,
                                                               messageDate,
                                                               sendPrompt,
                                                               question,
                                                               itemIndex,
                                                               turnId,
                                                               feedback,
                                                               streaming
                                                           }) {
    const local = (date) => dayjs.utc(date).tz(dayjs.tz.guess());
    const isUser = role === "user";
    const date = local(messageDate).format("HH:mm");
    const day = local(messageDate).format("DD");
    const month = local(messageDate).format("MMMM");
    const [likes, setLikes] = useState(0);
    const [dislikes, setDislikes] = useState(0);
    const [action, setAction] = useState(null);
    const [copied, setCopied] = useState(false);
    const text = isUser ? content : displayedContent || content;

    const sendFeedBack = (turnId, action, callback) => {
        const requestData = {
            id: turnId,
            value:
                action === "like"
                    ? likes === 1
                        ? 0
                        : 1
                    : dislikes === 1
                        ? 0
                        : -1
        };

        axios
            .post(process.env.REACT_APP_API_URL + "Conversation/SendFeedback", requestData)
            .then(() => callback && callback())
            .catch(handleRequestErrors);
    };

    const like = () => {
        sendFeedBack(turnId, "like", () => {
            setLikes(likes === 1 ? 0 : 1);
            setDislikes(0);
            setAction("liked");
        });
    };

    const dislike = () => {
        sendFeedBack(turnId, "dislike", () => {
            setLikes(0);
            setDislikes(dislikes === 1 ? 0 : 1);
            setAction("disliked");
        });
    };

    const copyText = async () => {
        setCopied(true);
        await navigator.clipboard.writeText(text);
        setTimeout(() => setCopied(false), 2000);
        message.success("Copied");
    };

    useEffect(() => {
        if (feedback) {
            if (feedback.value === 1) {
                setLikes(1);
                setDislikes(0);
                setAction("liked");
            } else if (feedback.value === -1) {
                setLikes(0);
                setDislikes(1);
                setAction("disliked");
            }
        }
    }, [feedback]);

    const actions = [
        <Tooltip key="copy" title="Copy">
            <div onClick={copyText} className="curs-pointer">
                {!copied ? <CopyOutlined /> : <CheckOutlined />}
            </div>
        </Tooltip>,
        <Tooltip key="like" title="Good response">
            <div
                className={`curs-pointer content-action ${likes > 0 ? "liked-item" : ""}`}
                onClick={like}
            >
                {React.createElement(likes > 0 ? LikeFilled : LikeOutlined)}
                <span>{likes}</span>
            </div>
        </Tooltip>,
        <Tooltip key="dislike" title="Bad response">
            <div
                className={`curs-pointer content-action ${dislikes > 0 ? "disliked-item" : ""}`}
                onClick={dislike}
            >
                {React.createElement(action === "disliked" ? DislikeFilled : DislikeOutlined)}
                <span>{dislikes}</span>
            </div>
        </Tooltip>,
        <Tooltip key="refresh" title="Refresh">
      <span className="curs-pointer" onClick={() => sendPrompt(question)}>
        <UndoOutlined />
      </span>
        </Tooltip>
    ];

    return (
        <div className={`chat-message ${isUser ? "user" : "assistant"}`}>
            <div className="d-flex">
                {!isUser && (
                    <Avatar className="assistant-avatar" src={byblosAvatar} size="small" />
                )}
                <div className="message-bubble">
                    {!isUser && (
                        <div className="assistant-header">
                            <span className="assistant-name">ByblosGPT</span>
                            <span className="message-date">{`${date}, ${day} ${month}`}</span>
                        </div>
                    )}
                    {isUser && <Text className="message-text">{text}</Text>}
                </div>
            </div>

            {!isUser && (
                <Text className="message-text">
                    <div className="markdown">
                        <ReactMarkdown
                            key={itemIndex}
                            remarkPlugins={[remarkGfm]}
                            rehypePlugins={[rehypeRaw]}
                            components={{ p: ({ children }) => <span>{children}</span> }}
                        >
                            {text}
                        </ReactMarkdown>
                    </div>
                </Text>
            )}

            {!isUser && !streaming && (
                <div className="content-actions">{actions}</div>
            )}
        </div>
    );
});

export default function ChatUI() {
    const [form] = Form.useForm()
    const guid = uuidv4();
    const [messages, setMessages] = useState([]);
    const messageExist = messages.length > 0;
    const [userQuestion,setUserQuestion] = useState('')
    const chatRef = useRef(null);
    const [loading, setLoading] = useState(false);
    const [collapsed, setCollapsed] = useState(false);
    const [mobileMenuVisible,setMobileMenuVisible] = useState(false)
    const [streaming,setStreaming] = useState(false)
    const [historyData,setHistoryData] = useState([])
    const [activeConversationId,setActiveConversationId] = useState(0)
    const abortControllerRef = useRef(null);
    const [historyLoading, setHistoryLoading] = useState(false);
    let userId = localStorage.getItem('userId');
    let token = localStorage.getItem('token');

    useEffect(() => {
        localStorage.setItem("sessionId", guid);
        getConversationsHistory()
    }, []);

    useEffect(() => {
        if(!token) {
            getToken()
        }
    }, [token]);

    const getToken = () => {
        axios.get(process.env.REACT_APP_IDENTITY_URL + `Token/GenerateTokenByUserId?userId=${userId}`)
            .then(res => {
                localStorage.setItem('token',res.data.token)
            })
            .catch(err => {
                handleRequestErrors(err)
                setHistoryLoading(false)
            })
    }

    useEffect(() => {
        chatRef.current?.scrollTo(0, chatRef.current.scrollHeight);
    }, [messages]);

    const clearChat = () => {
        localStorage.removeItem("sessionId");
        setMessages([]);
        localStorage.setItem("sessionId", guid);
        setActiveConversationId(0)
        getConversationsHistory()
        form.setFieldsValue({
            message : ''
        })
    };

    const optionsAnswer = (answer) => {
        form.setFieldsValue({
            message : answer
        })
        sendPrompt(answer)
    }

    const items = [
        { key: "1", label:
                <div onClick={() => optionsAnswer('Translate to Russian')}>
                    <img alt={'flagRus'} className={'dropdown-flag'} src={flagRus}/>
                    Russian
                </div>
        },
        {
            key: "2", label:
                <div onClick={() => optionsAnswer('Translate to Armenian')}>
                    <img alt={'flagArm'} className={'dropdown-flag'} src={armFlag}/>
                    Armenian
                </div>
        },
        {
            key: "3", label:
                <div onClick={() => optionsAnswer('Translate to English')}>
                    <img alt={'flagENG'} className={'dropdown-flag'} src={flagEng}/>
                    English
                </div>
        },
    ];


    const TYPING_UPDATE_INTERVAL = 50;

    const sendPrompt = async (question = form.getFieldValue("message")) => {
        if (!question?.trim() || loading || streaming) return;

        setLoading(true);

        const userMessage = { role: "user", content: question, id: uuidv4() };
        setMessages((prev) => [...prev, userMessage]);
        form.setFieldsValue({ message: "" });
        setUserQuestion(question);

        let loadingText;
        if (/[ա-ֆԱ-Ֆ]/.test(question)) loadingText = "Մեկ վայրկյան, ես պատրաստում եմ պատասխանը․․․";
        else if (/[а-яА-ЯёЁ]/.test(question)) loadingText = "Секундочку, я готовлю ответ․․․";
        else loadingText = "Give me a second to prepare your answer․․․";

        let assistantMessage = {
            role: "assistant",
            content: loadingText,
            displayedContent: "",
            fullStream: "",
            streaming: true,
            id: uuidv4(),
        };

        setMessages((prev) => [...prev, assistantMessage]);
        const sessionId = localStorage.getItem("sessionId");
        const url = process.env.REACT_APP_API_URL + `Query/prompt/stream2?query=${encodeURIComponent(question)}&sessionId=${sessionId}&token=${token}`;
        try {
            const es = new EventSource(url, { withCredentials: true });

            abortControllerRef.current = es;

            setStreaming(true);
            assistantMessage.displayedContent = "";
            setMessages((prev) => {
                const updated = [...prev];
                updated[updated.length - 1] = { ...assistantMessage };
                return updated;
            });
            let lastUpdate = Date.now();
            let chunkBuffer = "";

            const flushBufferToUI = () => {
                if (!chunkBuffer) return;
                assistantMessage.displayedContent += chunkBuffer;
                chunkBuffer = "";

                setMessages((prev) => {
                    const updated = [...prev];
                    updated[updated.length - 1] = { ...assistantMessage };
                    return updated;
                });

                lastUpdate = Date.now();
            };

            es.addEventListener("token", (event) => {
                if (!event?.data) return;

                // DO NOT append into content!
                assistantMessage.fullStream += event.data;
                chunkBuffer += event.data;

                if (Date.now() - lastUpdate > TYPING_UPDATE_INTERVAL) {
                    flushBufferToUI();
                }
            });

            es.addEventListener("meta", (event) => {
                try {
                    const meta = JSON.parse(event.data);
                    assistantMessage.turnId = meta.turnId;
                } catch (e) {
                    console.warn("meta parse failed", e, event.data);
                }
            });

            es.addEventListener("done", () => {
                flushBufferToUI();

                // replace loading text with actual response
                assistantMessage.content = assistantMessage.displayedContent;
                assistantMessage.streaming = false;
                assistantMessage.messageDate = dayjs();

                setMessages((prev) => {
                    const updated = [...prev];
                    updated[updated.length - 1] = { ...assistantMessage };
                    return updated;
                });

                setStreaming(false);
                setLoading(false);
                getConversationsHistory();

                try { es.close(); } catch {}
                abortControllerRef.current = null;
            });

            // Generic error handler — log more details for debugging
            es.onerror = (err) => {
                // readyState: 0 = connecting, 1 = open, 2 = closed
                console.error("SSE error (es.onerror)", err, "readyState=", es.readyState);

                // show a more informative message to the user (helps debug CORS/HTTPS)
                message.error("Connection error during response streaming — check console for details");

                // finalize UI state: mark assistant streaming false and leave whatever text we have
                assistantMessage.displayedContent = assistantMessage.displayedContent || assistantMessage.content;
                assistantMessage.streaming = false;
                assistantMessage.messageDate = dayjs();


                setStreaming(false);
                setLoading(false);

                try { es.close(); } catch (e) {}
                abortControllerRef.current = null;
            };

        } catch (err) {
            console.error("SSE create error:", err);
            message.error("Could not open streaming connection");
        }
    };

    const getConversationsHistory = () => {
        setHistoryLoading(true)
        axios.get(process.env.REACT_APP_API_URL + `Conversation/GetAll`)
            .then(res => {
                setHistoryData(res.data)
                setHistoryLoading(false)
            })
            .catch(err => {
                handleRequestErrors(err)
                setHistoryLoading(false)
            })
    }

    const stopGenerating = () => {
        const es = abortControllerRef.current;
        if (es) {
            try { es.close(); } catch(e) {}
            abortControllerRef.current = null;
        }

        setStreaming(false);
        setLoading(false);

        setMessages((prev) => {
            const updated = [...prev];
            const last = updated[updated.length - 1];
            if (last?.streaming) {
                updated[updated.length - 1] = {
                    ...last,
                    streaming: false,
                    content: last.displayedContent || last.content
                };
            }
            return updated;
        });
    };

    const getConversationById = (itemId) => {
        stopGenerating();
        axios.get(process.env.REACT_APP_API_URL + `Conversation/GetById?id=${itemId}`)
            .then(res => {
                const messages = []
                localStorage.setItem('sessionId',res.data.sessionId)
                res.data.turns.map((item) => {
                    const userMessage = { role: "user", content: item.question,id : uuidv4() };
                    const assistantMessage = { role: "assistant", content: item.answer, displayedContent: "", streaming: false,id : uuidv4(),messageDate : item.createdUtc,turnId : item.id,feedback : item.feedback};
                    messages.push(userMessage)
                    messages.push(assistantMessage)
                })
                setStreaming(false)
                stopGenerating()
                setMobileMenuVisible(false)
                setMessages(messages)
                form.setFieldsValue({
                    message : ''
                })
                window.scrollTo({
                    top: document.body.scrollHeight,
                    behavior: 'smooth'
                });
            })
            .catch(err => {
                handleRequestErrors(err)
                setHistoryLoading(false)
            })
    }

    useEffect(() => {
        getConversationsHistory()
    }, []);

    return (
        <Layout style={{height: "100vh", overflow: 'hidden'}}>
            <Sider
                width={260}
                collapsible
                mode="inline"
                collapsed={collapsed}
                trigger={null}
                className="chat-sider"
            >
                <div className={'sider-inner'} style={{alignItems: collapsed ? 'center' : 'flexStart'}}>
                    <div className="bank-header curs-pointer" onClick={clearChat}>
                        <img src={!collapsed ? sidebarLogo : collapsedLogo} alt="logo" height="24"/>
                    </div>
                    <div className="chat-history">
                        <div className="collapse-toggle">
                            <Button
                                type="text"
                                icon={collapsed ? <MenuUnfoldOutlined/> : <MenuFoldOutlined/>}
                                onClick={() => setCollapsed(!collapsed)}
                                className="collapse-btn"
                            />
                        </div>
                        <Button
                            icon={<FormOutlined/>}
                            block
                            className={`new-chat ${collapsed ? "collapsed" : ""}`}
                            onClick={clearChat}
                        >
                            New chat
                        </Button>
                        <Spin className={'mb-2'} spinning={historyLoading}>
                            <ConversationsHistory
                                streaming={streaming}
                                activeConversationId={activeConversationId}
                                setActiveConversationId={setActiveConversationId}
                                getConversationById={getConversationById}
                                messageExist={messageExist}
                                loading={historyLoading}
                                collapsed={collapsed}
                                historyData={historyData}
                            />
                        </Spin>
                    </div>
                </div>
                {/* 👇 Footer links */}
                <div className="sider-footer">
                    <Tooltip title={'Help'}>
                        <Space size={6} className={`help-wrapper ${collapsed ? 'collapsed' : ''}`}>
                            <QuestionCircleOutlined style={{fontSize: '16px'}}/>
                            <a className={`${collapsed ? 'collapsed' : ''}`} href="/help">Help</a>
                        </Space>
                    </Tooltip>
                    <Tooltip title={'Settings'}>
                        <Space className={`help-wrapper ${collapsed ? 'collapsed' : ''}`}>
                            <SettingOutlined style={{fontSize: '16px'}}/>
                            <a className={`${collapsed ? 'collapsed' : ''}`} href="/privacy">Settings</a>
                        </Space>
                    </Tooltip>
                    <Space size={10} className={`terms ${collapsed ? 'collapsed' : ''} `}>
                        <a href="/terms">Terms of Use</a>
                        <span>or</span>
                        <a href="/terms">Privacy policy</a>
                    </Space>
                </div>
            </Sider>

            <Drawer
                title={
                    <div className="drawer-header">
                        <img src={sidebarLogo} alt="logo" height="24"/>
                    </div>
                }
                placement="left"
                closable={true}
                onClose={() => setMobileMenuVisible(false)}
                open={mobileMenuVisible}
                width={260}
                className="mobile-drawer"
            >
                <div className={'mobile-drawer-content'}>
                    <div className="chat-history">
                        <Button
                            icon={<FormOutlined/>}
                            block
                            className="new-chat"
                            onClick={clearChat}
                        >
                            New chat
                        </Button>
                        <Spin className={'mb-2'} spinning={historyLoading}>
                            <ConversationsHistory
                                streaming={streaming}
                                activeConversationId={activeConversationId}
                                setActiveConversationId={setActiveConversationId}
                                getConversationById={getConversationById}
                                messageExist={messageExist}
                                loading={historyLoading}
                                collapsed={collapsed}
                                historyData={historyData}
                            />
                        </Spin>
                    </div>
                    <div className="sider-footer">
                        <div>
                            <Space size={6} className={'help-wrapper'}>
                                <QuestionCircleOutlined style={{fontSize: '16px'}}/>
                                <a className={`${collapsed ? 'collapsed' : ''}`} href="/help">Help</a>
                            </Space>
                            <Space className={'help-wrapper'}>
                                <SettingOutlined style={{fontSize: '16px'}}/>
                                <a className={`${collapsed ? 'collapsed' : ''}`} href="/privacy">Settings</a>
                            </Space>
                        </div>

                        <Space size={10} className={'terms'}>
                            <a href="/terms">Terms of Use</a>
                            <span>or</span>
                            <a href="/terms">Privacy policy</a>
                        </Space>
                    </div>
                </div>
            </Drawer>

            <Layout>
                {/* Mobile Menu Button */}
                <div className="mobile-header">
                    <div className={'d-flex align-center'}>
                        <Button
                            type="text"
                            icon={<MenuUnfoldOutlined/>}
                            onClick={() => setMobileMenuVisible(true)}
                            className="mobile-menu-btn"
                        />
                        <Typography.Title level={5} className="mobile-title">ByblosGPT</Typography.Title>
                    </div>
                    <FormOutlined className={'mobile-new-chat'} onClick={clearChat}/>
                </div>
                <Content className="chat-content" ref={chatRef} style={{overflow: streaming ? 'hidden' : ''}}>
                    <div className="chat-center">
                        {messages.map((m, i) => (
                            <React.Fragment key={m.id}>
                                <ChatMessage {...m} sendPrompt={sendPrompt} question={userQuestion}/>
                            </React.Fragment>
                        ))}
                    </div>
                </Content>

                <div className="chat-footer" style={!messageExist ? {height: "100%"} : {}}>
                    <div className="chat-center">
                        {!messageExist && (
                            <div className="welcome-text">
                                <Typography.Title>Hi, I'm ByblosGPT.</Typography.Title>
                                <p>How can I help you today?</p>
                            </div>
                        )}
                        {
                            messageExist && <div className="quick-suggestions">
                                <Dropdown
                                    overlayStyle={{width: '140px'}}
                                    className={'fast-actions'}
                                    menu={{items}}
                                    placement={'topLeft'}
                                    arrow={{pointAtCenter: false}}
                                >
                                    <div className={'curs-pointer'}>
                                        <Space>
                                            <TranslationOutlined style={{color: '#CECFD2'}}/>
                                            Translate
                                        </Space>
                                    </div>
                                </Dropdown>

                                <div className={'fast-actions curs-pointer'}
                                     onClick={() => sendPrompt('Improve description')}>
                                    <Space>
                                        <StarOutlined style={{color: '#CECFD2'}}/>
                                        Improve
                                    </Space>
                                </div>

                                <div className={'fast-actions curs-pointer'}
                                     onClick={() => sendPrompt('Please provide more details')}>
                                    <Space>
                                        <ColumnWidthOutlined style={{color: '#CECFD2'}}/>
                                        Make longer
                                    </Space>
                                </div>
                            </div>
                        }
                        <div className="chat-input-wrapper">
                            <Dropdown menu={{items}}>
                                <div className="chat-input-icon left">
                                    <PlusOutlined/>
                                </div>
                            </Dropdown>
                            <Form name={'message-form'} form={form} className={'chat-message-form w-100'}>
                                <Form.Item name={'message'}>
                                    <Input.TextArea
                                        autoSize={{minRows: 1}}
                                        className="message-input"
                                        onPressEnter={(e) => {
                                            if (!e.shiftKey && !loading) {
                                                e.preventDefault();
                                                sendPrompt();
                                            }
                                        }}
                                        placeholder="How can I..."
                                    />
                                </Form.Item>
                            </Form>
                            <div className="chat-input-icon right">
                                <AudioOutlined/>
                                {!loading ? (
                                    <Button
                                        loading={loading}
                                        className="send-button"
                                        type="primary"
                                        shape="circle"
                                        icon={<ArrowUpOutlined/>}
                                        onClick={() => sendPrompt()}
                                    />
                                ) : (
                                    <Button
                                        className="send-button stop-answer"
                                        type="primary"
                                        shape="circle"
                                        icon={<BorderOutlined/>}
                                        onClick={stopGenerating}
                                    />
                                )}
                            </div>
                        </div>
                        {
                            !messageExist &&
                            <Space className={'related-answers'} size={8}>
                                <Button onClick={() => sendPrompt('Can I freeze my card?')}
                                        icon={<CreditCardOutlined/>}>
                                    Can I freeze my card?
                                </Button>
                                <Button icon={<BankOutlined/>} onClick={() => sendPrompt('What’s my balance?')}>
                                    What’s my balance?
                                </Button>
                                <Button icon={<SlidersOutlined/>}
                                        onClick={() => sendPrompt('Am I eligible for a loan?')}>
                                    Am I eligible for a loan?
                                </Button>
                                <Button icon={<PercentageOutlined/>} onClick={() => sendPrompt('Any current offers?')}>
                                    Any current offers?
                                </Button>
                                <Button icon={<GoldOutlined/>} onClick={() => sendPrompt('How to deposit coins?')}>
                                    How to deposit coins?
                                </Button>
                                <Button icon={<DollarOutlined/>} onClick={() => sendPrompt(' What’s the cash limit?')}>
                                    What’s the cash limit?
                                </Button>
                            </Space>
                        }
                    </div>
                </div>
            </Layout>
        </Layout>
    );
}
